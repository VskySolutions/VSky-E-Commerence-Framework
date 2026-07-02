using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Categories;

/// <summary>Returns a page of categories ordered by display order then name, optionally filtered by a name search term.</summary>
public record ListCategoriesQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PaginatedList<CategoryDto>>;

public class ListCategoriesQueryHandler : IRequestHandler<ListCategoriesQuery, PaginatedList<CategoryDto>>
{
    private readonly IApplicationDbContext _db;

    public ListCategoriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<CategoryDto>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Category> query = _db.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c => c.Name.Contains(term));
        }

        var ordered = query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name);

        var page = await PaginatedList<Category>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(CategoryDto.From).ToList();
        return new PaginatedList<CategoryDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
