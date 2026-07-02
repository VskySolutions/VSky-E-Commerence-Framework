using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Orders;

/// <summary>
/// Admin list of all orders, newest first, optionally filtered by status name (e.g. "Unrouted" to
/// surface orders that no store could fulfil).
/// </summary>
public record ListOrdersQuery(string? Status, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedList<OrderSummaryDto>>;

public class ListOrdersQueryHandler : IRequestHandler<ListOrdersQuery, PaginatedList<OrderSummaryDto>>
{
    private readonly IApplicationDbContext _db;

    public ListOrdersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<OrderSummaryDto>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Order> query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Lines);

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        var ordered = query.OrderByDescending(o => o.PlacedOnUtc);
        var page = await PaginatedList<Order>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(OrderSummaryDto.From).ToList();
        return new PaginatedList<OrderSummaryDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
