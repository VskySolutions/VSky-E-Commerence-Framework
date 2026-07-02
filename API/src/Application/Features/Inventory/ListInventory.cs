using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Inventory;

/// <summary>Returns a page of inventory levels, optionally filtered by product and/or store.</summary>
public record ListInventoryQuery(Guid? ProductId = null, Guid? StoreId = null, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedList<InventoryLevelDto>>;

public class ListInventoryQueryHandler : IRequestHandler<ListInventoryQuery, PaginatedList<InventoryLevelDto>>
{
    private readonly IApplicationDbContext _db;

    public ListInventoryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<InventoryLevelDto>> Handle(ListInventoryQuery request, CancellationToken cancellationToken)
    {
        IQueryable<InventoryLevel> query = _db.InventoryLevels.AsNoTracking();

        if (request.ProductId is Guid productId)
            query = query.Where(l => l.ProductId == productId);

        if (request.StoreId is Guid storeId)
            query = query.Where(l => l.StoreId == storeId);

        var ordered = query.OrderBy(l => l.ProductId).ThenBy(l => l.StoreId);

        var page = await PaginatedList<InventoryLevel>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(InventoryLevelDto.From).ToList();
        return new PaginatedList<InventoryLevelDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
