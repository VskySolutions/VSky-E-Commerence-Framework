using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.TaxCategories;

/// <summary>
/// Returns a page of tax categories ordered by display order then name, optionally filtered by name
/// or restricted to active categories. Backs the product form's Tax Category picker (AC-CAT-001.6).
/// </summary>
public record ListTaxCategoriesQuery(int Page = 1, int PageSize = 200, string? Search = null, bool? ActiveOnly = null)
    : IRequest<PaginatedList<TaxCategoryDto>>;

public class ListTaxCategoriesQueryHandler : IRequestHandler<ListTaxCategoriesQuery, PaginatedList<TaxCategoryDto>>
{
    private readonly IApplicationDbContext _db;

    public ListTaxCategoriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<TaxCategoryDto>> Handle(ListTaxCategoriesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<TaxCategory> query = _db.TaxCategories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(t => t.Name.Contains(term));
        }

        if (request.ActiveOnly == true)
            query = query.Where(t => t.IsActive);

        var ordered = query.OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name);

        var page = await PaginatedList<TaxCategory>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(TaxCategoryDto.From).ToList();
        return new PaginatedList<TaxCategoryDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
