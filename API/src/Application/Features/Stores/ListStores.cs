using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Stores;

/// <summary>Returns a page of stores ordered by name, optionally filtered by a name search term.</summary>
public record ListStoresQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PaginatedList<StoreDto>>;

public class ListStoresQueryHandler : IRequestHandler<ListStoresQuery, PaginatedList<StoreDto>>
{
    private readonly IApplicationDbContext _db;

    public ListStoresQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<StoreDto>> Handle(ListStoresQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Store> query = _db.Stores.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(s => s.Name.Contains(term));
        }

        var ordered = query.OrderBy(s => s.Name);

        var page = await PaginatedList<Store>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(StoreDto.From).ToList();
        return new PaginatedList<StoreDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
