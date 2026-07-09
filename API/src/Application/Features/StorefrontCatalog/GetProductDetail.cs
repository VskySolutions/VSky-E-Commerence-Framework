using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.StorefrontCatalog;

/// <summary>
/// Returns the full published product detail (resolved by id or slug) with variants, product- and
/// variant-level media, specification option ids, tag names, and the related / cross-sell / up-sell
/// sections resolved to published summaries (AC-CAT-007.2). Throws <see cref="NotFoundException"/>
/// when the product is missing or unpublished.
/// </summary>
public record GetProductDetailQuery(string IdOrSlug) : IRequest<StorefrontProductDetailDto>;

public class GetProductDetailQueryHandler : IRequestHandler<GetProductDetailQuery, StorefrontProductDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetProductDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<StorefrontProductDetailDto> Handle(GetProductDetailQuery request, CancellationToken cancellationToken)
    {
        var key = request.IdOrSlug?.Trim() ?? string.Empty;

        // Product- and variant-level pictures both carry ProductId, so p.Pictures loads the full gallery.
        var query = _db.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Where(p => p.IsPublished)
            .Include(p => p.Variants).ThenInclude(v => v.AttributeValues)
                .ThenInclude(av => av.ProductAttributeValue).ThenInclude(pav => pav!.ProductAttribute)
            .Include(p => p.Pictures).ThenInclude(pic => pic.Media)
            .Include(p => p.SpecificationValues)
            .Include(p => p.Tags).ThenInclude(t => t.ProductTag)
            .Include(p => p.Relationships);

        var product = (Guid.TryParse(key, out var productId)
            ? await query.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            : await query.FirstOrDefaultAsync(p => p.Slug == key, cancellationToken))
            ?? throw new NotFoundException(nameof(Product), key);

        var dto = StorefrontProductDetailDto.From(product);

        // Resolve relationship targets to published summaries; unpublished/soft-deleted targets drop out.
        var relatedIds = product.Relationships.Select(r => r.RelatedProductId).Distinct().ToList();
        if (relatedIds.Count > 0)
        {
            var targets = await _db.Products
                .AsNoTracking()
                .Published()
                .Where(p => relatedIds.Contains(p.Id))
                .WithSummaryImages()
                .ToListAsync(cancellationToken);

            var byId = targets.ToDictionary(p => p.Id, StorefrontProductSummaryDto.From);

            List<StorefrontProductSummaryDto> Section(ProductRelationshipType type) => product.Relationships
                .Where(r => r.RelationshipType == type && byId.ContainsKey(r.RelatedProductId))
                .OrderBy(r => r.DisplayOrder)
                .Select(r => byId[r.RelatedProductId])
                .ToList();

            dto.RelatedProducts = Section(ProductRelationshipType.Related);
            dto.CrossSells = Section(ProductRelationshipType.CrossSell);
            dto.UpSells = Section(ProductRelationshipType.UpSell);
        }

        return dto;
    }
}
