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
    string? Reason = null,
    bool NotifyCustomer = true,
    bool RestockItems = true) : IRequest<PaymentDto>;

public class RefundOrderCommandHandler : IRequestHandler<RefundOrderCommand, PaymentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISender _mediator;
    private readonly IEmailTemplateSender _templates;
    private readonly IInventoryService _inventory;

    public RefundOrderCommandHandler(IApplicationDbContext db, ISender mediator, IEmailTemplateSender templates, IInventoryService inventory)
    {
        _db = db;
        _mediator = mediator;
        _templates = templates;
        _inventory = inventory;
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
        var result = await _mediator.Send(new RefundPaymentCommand(payment.Id, amount, request.Reason), cancellationToken);

        // Return the refunded units to stock (AC-CAT-011.5), the single restock path shared with cancellation.
        // A line-item refund restocks exactly those lines; a full refund (no amount and no line filter)
        // restocks the whole order; an amount-only partial refund can't be mapped to units, so it doesn't
        // restock. RMA-driven refunds pass RestockItems: false — the return flow already restocked the
        // received units via MarkAsReceivedAsync — so stock is never restored twice.
        if (request.RestockItems)
        {
            if (request.OrderLineItemIds is { Count: > 0 } restockLineIds)
                await _inventory.RestoreForOrderAsync(order, restockLineIds, cancellationToken);
            else if (request.Amount is null)
                await _inventory.RestoreForOrderAsync(order, ct: cancellationToken);
        }

        // Notify the customer their refund was issued, via the admin-editable template. Callers that already
        // message the customer (e.g. an RMA resolution) pass NotifyCustomer: false to avoid a duplicate.
        if (request.NotifyCustomer)
        {
            await _templates.SendAsync("order.refunded", order.ContactEmail ?? string.Empty, order.ContactName,
                new Dictionary<string, string>
                {
                    ["customerName"] = string.IsNullOrWhiteSpace(order.ContactName) ? "there" : order.ContactName!,
                    ["orderNumber"] = order.OrderNumber,
                    ["refundAmount"] = amount is decimal a ? $"{order.CurrencyCode} {a:0.00}" : "the refunded amount",
                }, cancellationToken);
        }

        return result;
    }
}
