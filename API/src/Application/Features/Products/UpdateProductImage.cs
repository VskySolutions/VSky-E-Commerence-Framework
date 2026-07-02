using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Products;

/// <summary>Updates a product/variant gallery entry (the route id wins over any id in the body).</summary>
public record UpdateProductImageCommand(
    Guid ImageId,
    ProductMediaType MediaType,
    string Url,
    string? ThumbnailUrl = null,
    string? AltText = null,
    int DisplayOrder = 0,
    Guid? ProductVariantId = null) : IRequest<ProductImageDto>;

public class UpdateProductImageCommandValidator : AbstractValidator<UpdateProductImageCommand>
{
    public UpdateProductImageCommandValidator()
    {
        RuleFor(x => x.ImageId).NotEmpty();
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.ThumbnailUrl).MaximumLength(2048);
        RuleFor(x => x.AltText).MaximumLength(400);
    }
}

public class UpdateProductImageCommandHandler : IRequestHandler<UpdateProductImageCommand, ProductImageDto>
{
    private readonly IApplicationDbContext _db;

    public UpdateProductImageCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ProductImageDto> Handle(UpdateProductImageCommand request, CancellationToken cancellationToken)
    {
        var image = await _db.ProductImages
            .FirstOrDefaultAsync(i => i.Id == request.ImageId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductImage), request.ImageId);

        if (request.ProductVariantId is Guid variantId
            && !await _db.ProductVariants.AnyAsync(v => v.Id == variantId && v.ProductId == image.ProductId, cancellationToken))
            throw new NotFoundException(nameof(ProductVariant), variantId);

        image.ProductVariantId = request.ProductVariantId;
        image.MediaType = request.MediaType;
        image.Url = request.Url;
        image.ThumbnailUrl = request.ThumbnailUrl;
        image.AltText = request.AltText;
        image.DisplayOrder = request.DisplayOrder;

        await _db.SaveChangesAsync(cancellationToken);
        return ProductImageDto.From(image);
    }
}
