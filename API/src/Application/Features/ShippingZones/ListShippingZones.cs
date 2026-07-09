using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.ShippingZones;

/// <summary>Returns a page of shipping zones ordered by name, optionally filtered by a name/country search term and/or enabled state.</summary>
public record ListShippingZonesQuery(int Page = 1, int PageSize = 20, string? Search = null, string? CountryCode = null, bool? IsEnabled = null)
    : IRequest<PaginatedList<ShippingZoneDto>>;

public class ListShippingZonesQueryHandler : IRequestHandler<ListShippingZonesQuery, PaginatedList<ShippingZoneDto>>
{
    private readonly IApplicationDbContext _db;

    public ListShippingZonesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<ShippingZoneDto>> Handle(ListShippingZonesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ShippingZone> query = _db.ShippingZones.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(z => z.Name.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.CountryCode))
        {
            var country = request.CountryCode.Trim();
            query = query.Where(z => z.CountryCode == country);
        }

        if (request.IsEnabled.HasValue)
            query = query.Where(z => z.IsEnabled == request.IsEnabled.Value);

        var ordered = query.OrderBy(z => z.Name);

        var page = await PaginatedList<ShippingZone>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(ShippingZoneDto.From).ToList();
        return new PaginatedList<ShippingZoneDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
