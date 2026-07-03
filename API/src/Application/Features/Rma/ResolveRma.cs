using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Features.Orders;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Rma;

/// <summary>
/// Approves or rejects a return (AC-ORD-004.3). On approval it restocks accepted units at the fulfilling
/// store and, when the resolution is a refund, processes it through the order's original gateway
/// (AC-ORD-004.4). Replacement / store-credit resolutions are recorded (downstream flows are future work).
/// </summary>
public record ResolveRmaCommand(
    Guid RmaId,
    bool Approve,
    RmaResolution Resolution = RmaResolution.None,
    decimal? RefundAmount = null,
    string? Notes = null) : IRequest<RmaDto>;

public class ResolveRmaCommandHandler : IRequestHandler<ResolveRmaCommand, RmaDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IInventoryService _inventory;
    private readonly ISender _mediator;
    private readonly IEmailEnqueuer _emails;
    private readonly IDateTimeProvider _clock;

    public ResolveRmaCommandHandler(
        IApplicationDbContext db, ICurrentUserService current, IInventoryService inventory,
        ISender mediator, IEmailEnqueuer emails, IDateTimeProvider clock)
    {
        _db = db;
        _current = current;
        _inventory = inventory;
        _mediator = mediator;
        _emails = emails;
        _clock = clock;
    }

    public async Task<RmaDto> Handle(ResolveRmaCommand request, CancellationToken cancellationToken)
    {
        var rma = await _db.Rmas
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == request.RmaId, cancellationToken)
            ?? throw new NotFoundException(nameof(Rma), request.RmaId);

        if (rma.Status != RmaStatus.Requested)
            throw new ConflictException($"This return is already {rma.Status} and cannot be resolved again.");

        var order = await _db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == rma.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), rma.OrderId);

        var now = _clock.UtcNow;

        if (!request.Approve)
        {
            rma.Status = RmaStatus.Rejected;
            rma.Resolution = RmaResolution.None;
            rma.ResolutionNotes = request.Notes;
            rma.ResolvedOnUtc = now;
            rma.ResolvedById = _current.UserId;
            await _db.SaveChangesAsync(cancellationToken);
            await NotifyAsync(order, rma, approved: false, cancellationToken);
            return RmaDto.From(rma);
        }

        // Approved: restock accepted units at the fulfilling store (the only return flow that adds stock).
        if (order.AssignedStoreId is Guid storeId)
        {
            foreach (var line in rma.Lines)
                await _inventory.MarkAsReceivedAsync(line.ProductId, line.ProductVariantId, storeId, line.Quantity, cancellationToken);
        }

        if (request.Resolution == RmaResolution.Refund)
        {
            // Amount: explicit, else the returned units' value from the order lines.
            var amount = request.RefundAmount;
            if (amount is null)
            {
                var unitPrices = order.Lines.ToDictionary(l => l.Id, l => l.UnitPrice);
                amount = rma.Lines.Sum(l => (unitPrices.TryGetValue(l.OrderLineItemId, out var up) ? up : 0m) * l.Quantity);
            }

            if (amount > 0)
            {
                await _mediator.Send(new RefundOrderCommand(order.Id, null, amount, $"RMA {rma.RmaNumber}"), cancellationToken);
                rma.RefundedAmount = amount;
            }
        }

        rma.Status = RmaStatus.Completed;
        rma.Resolution = request.Resolution;
        rma.ResolutionNotes = request.Notes;
        rma.ResolvedOnUtc = now;
        rma.ResolvedById = _current.UserId;
        await _db.SaveChangesAsync(cancellationToken);

        await NotifyAsync(order, rma, approved: true, cancellationToken);
        return RmaDto.From(rma);
    }

    private async Task NotifyAsync(Order order, Domain.Entities.Rma rma, bool approved, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(order.ContactEmail))
            return;

        var outcome = approved
            ? $"has been approved (resolution: {rma.Resolution})."
            : "could not be approved.";
        await _emails.EnqueueAsync(
            "RmaResolved",
            order.ContactEmail!,
            order.ContactName,
            $"Update on your return {rma.RmaNumber}",
            $"Hi {order.ContactName},\n\nYour return {rma.RmaNumber} for order {order.OrderNumber} {outcome}\n\n" +
            (string.IsNullOrWhiteSpace(rma.ResolutionNotes) ? string.Empty : $"Notes: {rma.ResolutionNotes}\n\n") +
            "Thank you for shopping with us.",
            cancellationToken: ct);
    }
}
