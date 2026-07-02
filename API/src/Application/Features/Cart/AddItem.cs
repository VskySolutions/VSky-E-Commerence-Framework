using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Cart;

/// <summary>
/// Adds a product (optionally a specific variant) to the caller's cart, snapshotting the current unit
/// price (AC-CHK-001.1). When a line for the same product/variant already exists its quantity is
/// incremented rather than duplicated. <see cref="SessionId"/> identifies a guest cart.
/// </summary>
public record AddItemCommand(
    Guid ProductId,
    Guid? ProductVariantId,
    int Quantity,
    string? SessionId = null) : IRequest<CartDto>;

public class AddItemCommandValidator : AbstractValidator<AddItemCommand>
{
    public AddItemCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
    }
}

public class AddItemCommandHandler : IRequestHandler<AddItemCommand, CartDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public AddItemCommandHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<CartDto> Handle(AddItemCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        if (!product.IsPublished)
            throw new ConflictException("This product is not available for purchase.");

        decimal? variantPrice = null;
        if (request.ProductVariantId is Guid variantId)
        {
            var variant = await _db.ProductVariants
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken)
                ?? throw new NotFoundException(nameof(ProductVariant), variantId);

            if (variant.ProductId != product.Id)
                throw new ConflictException("The selected variant does not belong to the specified product.");
            if (!variant.IsEnabled)
                throw new ConflictException("The selected variant is not available for purchase.");

            variantPrice = variant.Price;
        }

        // Snapshot the price at the moment of adding (AC-CHK-001.1): variant price wins, else product price, else 0.
        var unitPrice = variantPrice ?? product.Price ?? 0m;

        var cart = await CartResolver.ResolveOrCreateAsync(_db, _current, request.SessionId, cancellationToken);

        var existing = cart.Items.FirstOrDefault(
            i => i.ProductId == request.ProductId && i.ProductVariantId == request.ProductVariantId);

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                Quantity = request.Quantity,
                UnitPrice = unitPrice,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await CartResolver.BuildDtoAsync(_db, cart, cancellationToken);
    }
}
