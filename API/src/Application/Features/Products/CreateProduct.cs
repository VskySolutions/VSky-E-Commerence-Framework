using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Products;

/// <summary>Creates a catalog product of any type. A Tax Category is mandatory (AC-CAT-001.6).</summary>
public record CreateProductCommand(
    string Name,
    ProductType ProductType,
    Guid TaxCategoryId,
    string? Slug = null,
    string? ShortDescription = null,
    string? FullDescription = null,
    string? Sku = null,
    decimal? Price = null,
    int StockQuantity = 0,
    bool AllowBackorder = false,
    DateTime? EstimatedRestockDate = null,
    Guid? ManufacturerId = null,
    bool IsPublished = false,
    bool ReviewsEnabled = true,
    int DisplayOrder = 0,
    int? DownloadExpiryDays = null,
    int? DownloadLimit = null,
    GiftCardType? GiftCardType = null,
    decimal? GiftCardAmount = null) : IRequest<ProductDto>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(400);
        RuleFor(x => x.TaxCategoryId).NotEmpty();
        RuleFor(x => x.Slug).MaximumLength(400);
        RuleFor(x => x.Sku).MaximumLength(400);
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _db;

    public CreateProductCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.TaxCategories.AnyAsync(t => t.Id == request.TaxCategoryId, cancellationToken))
            throw new NotFoundException(nameof(TaxCategory), request.TaxCategoryId);

        if (request.ManufacturerId is Guid manufacturerId
            && !await _db.Manufacturers.AnyAsync(m => m.Id == manufacturerId, cancellationToken))
            throw new NotFoundException(nameof(Manufacturer), manufacturerId);

        var entity = new Product
        {
            Name = request.Name,
            Slug = request.Slug,
            ProductType = request.ProductType,
            ShortDescription = request.ShortDescription,
            FullDescription = request.FullDescription,
            Sku = request.Sku,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            AllowBackorder = request.AllowBackorder,
            EstimatedRestockDate = request.EstimatedRestockDate,
            TaxCategoryId = request.TaxCategoryId,
            ManufacturerId = request.ManufacturerId,
            IsPublished = request.IsPublished,
            ReviewsEnabled = request.ReviewsEnabled,
            DisplayOrder = request.DisplayOrder,
            DownloadExpiryDays = request.DownloadExpiryDays,
            DownloadLimit = request.DownloadLimit,
            GiftCardType = request.GiftCardType,
            GiftCardAmount = request.GiftCardAmount,
        };

        _db.Products.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ProductDto.From(entity);
    }
}
