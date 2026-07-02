using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Products;

/// <summary>A single gallery-entry input for <see cref="ReplaceProductImagesCommand"/>.</summary>
public record ProductImageInput(
    Guid? ProductVariantId,
    ProductMediaType MediaType,
    string Url,
    string? ThumbnailUrl = null,
    string? AltText = null,
    int DisplayOrder = 0);

/// <summary>Replaces the entire image/video gallery of a product (both product- and variant-level) (REQ-CAT-012).</summary>
public record ReplaceProductImagesCommand(Guid ProductId, List<ProductImageInput> Images)
    : IRequest<List<ProductImageDto>>;

public class ReplaceProductImagesCommandValidator : AbstractValidator<ReplaceProductImagesCommand>
{
    public ReplaceProductImagesCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleForEach(x => x.Images).ChildRules(i =>
        {
            i.RuleFor(img => img.Url).NotEmpty().MaximumLength(2048);
            i.RuleFor(img => img.ThumbnailUrl).MaximumLength(2048);
            i.RuleFor(img => img.AltText).MaximumLength(400);
        });
    }
}

public class ReplaceProductImagesCommandHandler : IRequestHandler<ReplaceProductImagesCommand, List<ProductImageDto>>
{
    private readonly IApplicationDbContext _db;

    public ReplaceProductImagesCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ProductImageDto>> Handle(ReplaceProductImagesCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var inputs = request.Images ?? new List<ProductImageInput>();

        var variantIds = product.Variants.Select(v => v.Id).ToHashSet();
        foreach (var input in inputs)
        {
            if (input.ProductVariantId is Guid variantId && !variantIds.Contains(variantId))
                throw new NotFoundException(nameof(ProductVariant), variantId);
        }

        foreach (var existing in product.Images.ToList())
            product.Images.Remove(existing);

        foreach (var input in inputs)
        {
            product.Images.Add(new ProductImage
            {
                ProductId = product.Id,
                ProductVariantId = input.ProductVariantId,
                MediaType = input.MediaType,
                Url = input.Url,
                ThumbnailUrl = input.ThumbnailUrl,
                AltText = input.AltText,
                DisplayOrder = input.DisplayOrder,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return product.Images
            .OrderBy(i => i.DisplayOrder)
            .Select(ProductImageDto.From)
            .ToList();
    }
}
