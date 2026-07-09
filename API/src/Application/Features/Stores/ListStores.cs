using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Stores;

/// <summary>Returns a page of stores ordered by name, optionally filtered by a name search term, enabled state, guest-ordering and/or pickup availability.</summary>
public record ListStoresQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsEnabled = null,
    bool? GuestOrderingEnabled = null,
    bool? PickupEnabled = null) : IRequest<PaginatedList<StoreDto>>;

public class ListStoresQueryHandler : IRequestHandler<ListStoresQuery, PaginatedList<StoreDto>>
{
    private readonly IApplicationDbContext _db;

    public ListStoresQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<StoreDto>> Handle(ListStoresQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Store> query = _db.Stores.AsNoTracking().Include(s => s.Address);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(s => s.Name.Contains(term));
        }

        if (request.IsEnabled.HasValue)
            query = query.Where(s => s.IsEnabled == request.IsEnabled.Value);

        if (request.GuestOrderingEnabled.HasValue)
            query = query.Where(s => s.GuestOrderingEnabled == request.GuestOrderingEnabled.Value);

        if (request.PickupEnabled.HasValue)
            query = query.Where(s => s.PickupEnabled == request.PickupEnabled.Value);

        var ordered = query.OrderBy(s => s.Name);

        var page = await PaginatedList<Store>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(StoreDto.From).ToList();
        return new PaginatedList<StoreDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
