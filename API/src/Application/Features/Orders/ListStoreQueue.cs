using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Extensions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Orders;

/// <summary>
/// Returns the current store manager's queue: orders assigned to their store, newest first, optionally
/// filtered by status name (AC-STR-004.1).
/// </summary>
public record ListStoreQueueQuery(string? Status, int Page = 1, int PageSize = 20,
    string? SortBy = null, bool SortDescending = false)
    : IRequest<PaginatedList<OrderSummaryDto>>;

public class ListStoreQueueQueryHandler : IRequestHandler<ListStoreQueueQuery, PaginatedList<OrderSummaryDto>>
{
    // Column name (from the grid) -> entity property path. Anything else falls back to CreatedOnUtc desc.
    private static readonly IReadOnlyDictionary<string, string> SortMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["orderNumber"] = "OrderNumber",
        ["status"] = "Status",
        ["placedOnUtc"] = "PlacedOnUtc",
        ["totalAmount"] = "TotalAmount",
    };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public ListStoreQueueQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<PaginatedList<OrderSummaryDto>> Handle(ListStoreQueueQuery request, CancellationToken cancellationToken)
    {
        var storeId = await ResolveManagerStoreIdAsync(cancellationToken);

        IQueryable<Order> query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .Where(o => o.AssignedStoreId == storeId)
            // A store shouldn't see orders whose off-site payment was never completed.
            .ExcludeUnpaidRedirect();

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        var ordered = query.ApplySort(request.SortBy, request.SortDescending, SortMap,
            defaultSort: q => q.OrderByDescending(o => o.PlacedOnUtc));
        var page = await PaginatedList<Order>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(OrderSummaryDto.From).ToList();
        return new PaginatedList<OrderSummaryDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }

    private async Task<Guid> ResolveManagerStoreIdAsync(CancellationToken cancellationToken)
    {
        if (_current.UserId is not Guid userId)
            throw new ForbiddenAccessException("You are not assigned to a store.");

        var storeId = await _db.StoreManagerAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => a.StoreId)
            .FirstOrDefaultAsync(cancellationToken);

        if (storeId == Guid.Empty)
            throw new ForbiddenAccessException("You are not assigned to a store.");

        return storeId;
    }
}
