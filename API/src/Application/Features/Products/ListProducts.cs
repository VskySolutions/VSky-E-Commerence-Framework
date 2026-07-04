using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Products;

/// <summary>Returns a page of products ordered by display order then name, with optional filters.</summary>
public record ListProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    ProductType? Type = null,
    bool? IsPublished = null,
    Guid? CategoryId = null,
    Guid? ManufacturerId = null,
    bool? IsFeatured = null) : IRequest<PaginatedList<ProductListItemDto>>;

public class ListProductsQueryHandler : IRequestHandler<ListProductsQuery, PaginatedList<ProductListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public ListProductsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<ProductListItemDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Product> query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p => p.Name.Contains(term));
        }

        if (request.Type.HasValue)
            query = query.Where(p => p.ProductType == request.Type.Value);

        if (request.IsPublished.HasValue)
            query = query.Where(p => p.IsPublished == request.IsPublished.Value);

        if (request.IsFeatured.HasValue)
            query = query.Where(p => p.IsFeatured == request.IsFeatured.Value);

        if (request.ManufacturerId.HasValue)
            query = query.Where(p => p.ManufacturerId == request.ManufacturerId.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.ProductCategories.Any(c => c.CategoryId == request.CategoryId.Value));

        var ordered = query
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name);

        var page = await PaginatedList<Product>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(ProductListItemDto.From).ToList();
        return new PaginatedList<ProductListItemDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
