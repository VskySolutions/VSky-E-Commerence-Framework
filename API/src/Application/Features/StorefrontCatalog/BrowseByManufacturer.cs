using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.StorefrontCatalog;

/// <summary>
/// Returns a sortable page of published products for an enabled manufacturer (resolved by id or slug)
/// (AC-CAT-005.3). Throws <see cref="NotFoundException"/> when the manufacturer is missing or disabled.
/// </summary>
public record BrowseByManufacturerQuery(
    string IdOrSlug,
    int Page = 1,
    int PageSize = 20,
    string? Sort = null) : IRequest<PaginatedList<StorefrontProductSummaryDto>>;

public class BrowseByManufacturerQueryHandler : IRequestHandler<BrowseByManufacturerQuery, PaginatedList<StorefrontProductSummaryDto>>
{
    private readonly IApplicationDbContext _db;

    public BrowseByManufacturerQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<StorefrontProductSummaryDto>> Handle(BrowseByManufacturerQuery request, CancellationToken cancellationToken)
    {
        var key = request.IdOrSlug?.Trim() ?? string.Empty;

        var enabled = _db.Manufacturers.AsNoTracking().Where(m => m.IsEnabled);
        var manufacturer = (Guid.TryParse(key, out var manufacturerId)
            ? await enabled.FirstOrDefaultAsync(m => m.Id == manufacturerId, cancellationToken)
            : await enabled.FirstOrDefaultAsync(m => m.Slug == key, cancellationToken))
            ?? throw new NotFoundException(nameof(Manufacturer), key);

        var productsQuery = _db.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Published()
            .Where(p => p.ManufacturerId == manufacturer.Id)
            .WithSummaryImages()
            .ApplyStorefrontSort(request.Sort);

        var page = await PaginatedList<Product>.CreateAsync(productsQuery, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(StorefrontProductSummaryDto.From).ToList();
        return new PaginatedList<StorefrontProductSummaryDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
