using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.StorefrontCatalog;

/// <summary>
/// Returns a sortable page of published products carrying the given tag, resolved by slug or name
/// (AC-CAT-008.2). Throws <see cref="NotFoundException"/> when no such tag exists.
/// </summary>
public record GetProductsByTagQuery(
    string TagSlugOrName,
    int Page = 1,
    int PageSize = 20,
    string? Sort = null) : IRequest<PaginatedList<StorefrontProductSummaryDto>>;

public class GetProductsByTagQueryHandler : IRequestHandler<GetProductsByTagQuery, PaginatedList<StorefrontProductSummaryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetProductsByTagQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<StorefrontProductSummaryDto>> Handle(GetProductsByTagQuery request, CancellationToken cancellationToken)
    {
        var key = request.TagSlugOrName?.Trim() ?? string.Empty;

        var tag = await _db.ProductTags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == key || t.Name == key, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductTag), key);

        var productsQuery = _db.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Published()
            .Where(p => p.Tags.Any(tm => tm.ProductTagId == tag.Id))
            .WithSummaryImages()
            .ApplyStorefrontSort(request.Sort);

        var page = await PaginatedList<Product>.CreateAsync(productsQuery, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(StorefrontProductSummaryDto.From).ToList();
        return new PaginatedList<StorefrontProductSummaryDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
