using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.ProductAttributes;

/// <summary>Returns a page of product attributes ordered by display order then name, optionally filtered by a name search term.</summary>
public record ListProductAttributesQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PaginatedList<ProductAttributeDto>>;

public class ListProductAttributesQueryHandler : IRequestHandler<ListProductAttributesQuery, PaginatedList<ProductAttributeDto>>
{
    private readonly IApplicationDbContext _db;

    public ListProductAttributesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<ProductAttributeDto>> Handle(ListProductAttributesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ProductAttribute> query = _db.ProductAttributes
            .AsNoTracking()
            .Include(a => a.Values);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(a => a.Name.Contains(term));
        }

        var ordered = query.OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name);

        var page = await PaginatedList<ProductAttribute>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(ProductAttributeDto.From).ToList();

        // Populate the "In use" product count per attribute (drives the list column + delete protection).
        var ids = items.Select(i => i.Id).ToList();
        if (ids.Count > 0)
        {
            var usage = await _db.ProductAttributeMappings
                .AsNoTracking()
                .Where(m => ids.Contains(m.ProductAttributeId))
                .GroupBy(m => m.ProductAttributeId)
                .Select(g => new { AttributeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.AttributeId, x => x.Count, cancellationToken);

            foreach (var item in items)
                item.InUseCount = usage.TryGetValue(item.Id, out var count) ? count : 0;
        }

        return new PaginatedList<ProductAttributeDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
