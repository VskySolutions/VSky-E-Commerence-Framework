using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Features.Payments;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Orders;

/// <summary>
/// Issues a refund against an order — by selected line items, by explicit amount, or in full — through the
/// order's originating captured payment (AC-ORD-002.3/002.4). Delegates the gateway call and cumulative
/// accounting to <see cref="RefundPaymentCommand"/>.
/// </summary>
public record RefundOrderCommand(
    Guid OrderId,
    List<Guid>? OrderLineItemIds = null,
    decimal? Amount = null,
    string? Reason = null) : IRequest<PaymentDto>;

public class RefundOrderCommandHandler : IRequestHandler<RefundOrderCommand, PaymentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISender _mediator;

    public RefundOrderCommandHandler(IApplicationDbContext db, ISender mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<PaymentDto> Handle(RefundOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var payment = await _db.PaymentRecords
            .Where(p => p.OrderId == order.Id
                        && (p.Status == PaymentStatus.Captured || p.Status == PaymentStatus.PartiallyRefunded))
            .OrderByDescending(p => p.Amount)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException("This order has no captured payment available to refund.");

        var amount = request.Amount;
        if (amount is null && request.OrderLineItemIds is { Count: > 0 } lineIds)
        {
            var ids = lineIds.ToHashSet();
            amount = order.Lines.Where(l => ids.Contains(l.Id)).Sum(l => l.LineTotal);
            if (amount <= 0)
                throw new ConflictException("The selected lines have no refundable amount.");
        }

        // RefundPaymentCommand validates the amount against the remaining refundable balance, calls the
        // originating gateway, and records the cumulative refund + payment status.
        return await _mediator.Send(new RefundPaymentCommand(payment.Id, amount, request.Reason), cancellationToken);
    }
}
