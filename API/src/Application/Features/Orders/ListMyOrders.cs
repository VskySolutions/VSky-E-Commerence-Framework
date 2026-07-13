using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Orders;

/// <summary>
/// The authenticated customer's own order history (WO-45 buyer view), newest first. Scoped strictly to
/// orders owned by the caller's customer profile — a buyer can never see another customer's orders.
/// </summary>
public record ListMyOrdersQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedList<OrderSummaryDto>>;

public class ListMyOrdersQueryHandler : IRequestHandler<ListMyOrdersQuery, PaginatedList<OrderSummaryDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public ListMyOrdersQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<PaginatedList<OrderSummaryDto>> Handle(ListMyOrdersQuery request, CancellationToken cancellationToken)
    {
        if (_current.UserId is not Guid userId)
            throw new UnauthorizedException();

        var customerId = await _db.Customers
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenAccessException("The current user does not have a customer profile.");

        var ordered = _db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .Where(o => o.CustomerId == customerId)
            // Don't surface the buyer's own cancelled/abandoned redirect-payment attempts as orders.
            .ExcludeUnpaidRedirect()
            .OrderByDescending(o => o.PlacedOnUtc);

        var page = await PaginatedList<Order>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(OrderSummaryDto.From).ToList();
        return new PaginatedList<OrderSummaryDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
