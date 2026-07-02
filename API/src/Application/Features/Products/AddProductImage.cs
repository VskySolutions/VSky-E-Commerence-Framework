using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Products;

/// <summary>Adds an image or video-embed to a product or, when a variant is given, to that variant (REQ-CAT-012).</summary>
public record AddProductImageCommand(
    Guid ProductId,
    Guid? ProductVariantId,
    ProductMediaType MediaType,
    string Url,
    string? ThumbnailUrl = null,
    string? AltText = null,
    int DisplayOrder = 0) : IRequest<ProductImageDto>;

public class AddProductImageCommandValidator : AbstractValidator<AddProductImageCommand>
{
    public AddProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.ThumbnailUrl).MaximumLength(2048);
        RuleFor(x => x.AltText).MaximumLength(400);
    }
}

public class AddProductImageCommandHandler : IRequestHandler<AddProductImageCommand, ProductImageDto>
{
    private readonly IApplicationDbContext _db;

    public AddProductImageCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ProductImageDto> Handle(AddProductImageCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken))
            throw new NotFoundException(nameof(Product), request.ProductId);

        if (request.ProductVariantId is Guid variantId
            && !await _db.ProductVariants.AnyAsync(v => v.Id == variantId && v.ProductId == request.ProductId, cancellationToken))
            throw new NotFoundException(nameof(ProductVariant), variantId);

        var image = new ProductImage
        {
            ProductId = request.ProductId,
            ProductVariantId = request.ProductVariantId,
            MediaType = request.MediaType,
            Url = request.Url,
            ThumbnailUrl = request.ThumbnailUrl,
            AltText = request.AltText,
            DisplayOrder = request.DisplayOrder,
        };

        _db.ProductImages.Add(image);
        await _db.SaveChangesAsync(cancellationToken);
        return ProductImageDto.From(image);
    }
}
