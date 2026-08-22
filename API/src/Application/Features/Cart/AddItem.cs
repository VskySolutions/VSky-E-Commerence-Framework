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
    private readonly ICustomerGroupService _groups;
    private readonly ICommerceModeService _commerce;

    public AddItemCommandHandler(
        IApplicationDbContext db, ICurrentUserService current, ICustomerGroupService groups,
        ICommerceModeService commerce)
    {
        _db = db;
        _current = current;
        _groups = groups;
        _commerce = commerce;
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
        // This snapshot stays the BASE price — Customer Group pricing is overlaid at projection time
        // (AC-CUS-003.5) and re-resolved from this same base at checkout, so persisting a group price here
        // would apply a percentage rule twice (and would freeze a member's price against later group changes).
        var unitPrice = variantPrice ?? product.Price ?? 0m;

        var cart = await CartResolver.ResolveOrCreateAsync(_db, _current, request.SessionId, cancellationToken);

        // A cart is either all-buyable or all quote-only (REQ-INQ-001): the two check out through
        // different flows, so mixing them would mean one click producing both an order and an inquiry.
        // Skipped entirely when the whole tenant sells by inquiry — everything is quote-only there.
        if (!await _commerce.IsInquiryOnlyAsync(cancellationToken) && cart.Items.Count > 0)
        {
            var existingProductIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
            var cartHasInquiryItem = await _db.Products
                .AsNoTracking()
                .AnyAsync(p => existingProductIds.Contains(p.Id) && p.IsInquiryOnly, cancellationToken);

            if (cartHasInquiryItem != product.IsInquiryOnly)
            {
                throw new ConflictException(product.IsInquiryOnly
                    ? "Quote-only items must be requested on their own. Please empty your cart first, or " +
                      "check out the items already in it."
                    : "Your cart holds a quote-only item. Submit that request first, or empty your cart " +
                      "before adding items you can buy online.");
            }
        }

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
        return await CartResolver.BuildDtoAsync(_db, _groups, _commerce, cart, cancellationToken);
    }
}
