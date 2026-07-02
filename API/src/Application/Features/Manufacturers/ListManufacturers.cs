using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Manufacturers;

/// <summary>Returns a page of manufacturers ordered by display order then name, optionally filtered by a name search term.</summary>
public record ListManufacturersQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PaginatedList<ManufacturerDto>>;

public class ListManufacturersQueryHandler : IRequestHandler<ListManufacturersQuery, PaginatedList<ManufacturerDto>>
{
    private readonly IApplicationDbContext _db;

    public ListManufacturersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<ManufacturerDto>> Handle(ListManufacturersQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Manufacturer> query = _db.Manufacturers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(m => m.Name.Contains(term));
        }

        var ordered = query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name);

        var page = await PaginatedList<Manufacturer>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(ManufacturerDto.From).ToList();
        return new PaginatedList<ManufacturerDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
