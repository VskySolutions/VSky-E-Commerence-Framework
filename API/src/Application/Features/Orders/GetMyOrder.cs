using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Orders;

/// <summary>
/// The authenticated customer's view of one of their own orders (WO-45 buyer view), including line
/// items. Orders that do not belong to the caller's customer profile are reported as not found so the
/// endpoint never leaks the existence of another customer's order.
/// </summary>
public record GetMyOrderQuery(Guid Id) : IRequest<OrderDto>;

public class GetMyOrderQueryHandler : IRequestHandler<GetMyOrderQuery, OrderDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public GetMyOrderQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<OrderDto> Handle(GetMyOrderQuery request, CancellationToken cancellationToken)
    {
        if (_current.UserId is not Guid userId)
            throw new UnauthorizedException();

        var customerId = await _db.Customers
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenAccessException("The current user does not have a customer profile.");

        var order = await _db.Orders
            .Include(o => o.ShippingAddress)
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.Id && o.CustomerId == customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.Id);

        return OrderDto.From(order);
    }
}
