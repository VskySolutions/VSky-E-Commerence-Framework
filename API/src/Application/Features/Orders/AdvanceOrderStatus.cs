using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Orders;

/// <summary>
/// Advances an order along the WO-45 fulfilment lifecycle
/// (Pending → Processing → Shipped → Delivered, plus Cancelled from any non-terminal state).
/// Every transition is validated against the state machine, appended to the immutable
/// <see cref="OrderStatusHistory"/> (AC-ORD-001.3), and — on Shipped/Delivered — triggers a
/// customer notification email (AC-ORD-001.5). Shipping requires a tracking number and carrier
/// (AC-ORD-001.4), both of which are persisted on the order.
/// </summary>
public record AdvanceOrderStatusCommand(Guid OrderId, string ToStatus, string? TrackingNumber, string? Carrier)
    : IRequest<OrderDto>;

public class AdvanceOrderStatusCommandHandler : IRequestHandler<AdvanceOrderStatusCommand, OrderDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailEnqueuer _emails;
    private readonly ICurrentUserService _current;
    private readonly IDateTimeProvider _clock;

    public AdvanceOrderStatusCommandHandler(
        IApplicationDbContext db,
        IEmailEnqueuer emails,
        ICurrentUserService current,
        IDateTimeProvider clock)
    {
        _db = db;
        _emails = emails;
        _current = current;
        _clock = clock;
    }

    public async Task<OrderDto> Handle(AdvanceOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(request.ToStatus, ignoreCase: true, out var target))
            throw new ConflictException(
                $"'{request.ToStatus}' is not a recognised order status. " +
                "Allowed lifecycle targets: Processing, Shipped, Delivered, Cancelled.");

        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var from = order.Status;
        if (!IsValidTransition(from, target))
            throw new ConflictException($"An order in status '{from}' cannot transition to '{target}'.");

        // Shipping requires tracking details, captured on the order (AC-ORD-001.4).
        if (target == OrderStatus.Shipped)
        {
            if (string.IsNullOrWhiteSpace(request.TrackingNumber) || string.IsNullOrWhiteSpace(request.Carrier))
                throw new ConflictException("Shipping an order requires both a tracking number and a carrier.");

            order.TrackingNumber = request.TrackingNumber;
            order.ShippingCarrier = request.Carrier;
        }

        var now = _clock.UtcNow;
        order.Status = target;

        if (target == OrderStatus.Shipped)
            order.ShippedOnUtc = now;
        else if (target == OrderStatus.Delivered)
            order.DeliveredOnUtc = now;

        // Immutable audit entry for the transition (AC-ORD-001.3).
        _db.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = from,
            ToStatus = target,
            ChangedById = _current.UserId,
            ChangedOnUtc = now,
        });

        await _db.SaveChangesAsync(cancellationToken);

        // Customer notifications on shipment and delivery (AC-ORD-001.5).
        if (!string.IsNullOrWhiteSpace(order.ContactEmail))
        {
            if (target == OrderStatus.Shipped)
            {
                await _emails.EnqueueAsync(
                    "OrderShipped",
                    order.ContactEmail!,
                    order.ContactName,
                    $"Your order {order.OrderNumber} has shipped",
                    $"Hi {order.ContactName},\n\n" +
                    $"Good news — your order {order.OrderNumber} is on its way.\n" +
                    $"Carrier: {order.ShippingCarrier}\n" +
                    $"Tracking number: {order.TrackingNumber}\n\n" +
                    "Thank you for shopping with us.",
                    cancellationToken: cancellationToken);
            }
            else if (target == OrderStatus.Delivered)
            {
                await _emails.EnqueueAsync(
                    "OrderDelivered",
                    order.ContactEmail!,
                    order.ContactName,
                    $"Your order {order.OrderNumber} has been delivered",
                    $"Hi {order.ContactName},\n\n" +
                    $"Your order {order.OrderNumber} has been delivered.\n\n" +
                    "We hope you enjoy your purchase — thank you for shopping with us.",
                    cancellationToken: cancellationToken);
            }
        }

        return OrderDto.From(order);
    }

    /// <summary>
    /// The lifecycle state machine: a linear Pending → Processing → Shipped → Delivered progression,
    /// with Cancelled reachable from any non-terminal state (Delivered and Cancelled are terminal).
    /// </summary>
    private static bool IsValidTransition(OrderStatus from, OrderStatus to) => to switch
    {
        OrderStatus.Processing => from == OrderStatus.Pending,
        OrderStatus.Shipped => from == OrderStatus.Processing,
        OrderStatus.Delivered => from == OrderStatus.Shipped,
        OrderStatus.Cancelled => from is OrderStatus.Pending or OrderStatus.Processing or OrderStatus.Shipped,
        _ => false,
    };
}
