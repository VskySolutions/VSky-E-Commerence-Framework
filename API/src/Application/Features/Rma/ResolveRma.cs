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
    private readonly IStoreCreditService _storeCredit;
    private readonly ISender _mediator;
    private readonly IEmailEnqueuer _emails;
    private readonly IDateTimeProvider _clock;

    public ResolveRmaCommandHandler(
        IApplicationDbContext db, ICurrentUserService current, IInventoryService inventory,
        IStoreCreditService storeCredit, ISender mediator, IEmailEnqueuer emails, IDateTimeProvider clock)
    {
        _db = db;
        _current = current;
        _inventory = inventory;
        _storeCredit = storeCredit;
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

        // Resolution-specific downstream flow (AC-ORD-004.3/004.4).
        switch (request.Resolution)
        {
            case RmaResolution.Refund:
            {
                var amount = request.RefundAmount ?? ReturnedValue(order, rma);
                if (amount > 0)
                {
                    await _mediator.Send(new RefundOrderCommand(order.Id, null, amount, $"RMA {rma.RmaNumber}"), cancellationToken);
                    rma.RefundedAmount = amount;
                }
                break;
            }
            case RmaResolution.StoreCredit:
            {
                // Issue the returned value as store credit to the buyer's ledger balance.
                var amount = request.RefundAmount ?? ReturnedValue(order, rma);
                if (amount > 0)
                {
                    await _storeCredit.IssueAsync(
                        rma.CustomerId, amount, order.CurrencyCode,
                        $"Store credit for return {rma.RmaNumber}", rma.Id, order.Id, cancellationToken);
                    rma.StoreCreditIssued = amount;
                }
                break;
            }
            case RmaResolution.Replacement:
            {
                // Create a no-charge replacement order for the returned units, fulfilled from the same store.
                var replacement = CreateReplacementOrder(order, rma, now);
                _db.Orders.Add(replacement);
                rma.ReplacementOrderId = replacement.Id;
                break;
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

    /// <summary>The monetary value of the returned units, from the original order lines' unit prices.</summary>
    private static decimal ReturnedValue(Order order, Domain.Entities.Rma rma)
    {
        var unitPrices = order.Lines.ToDictionary(l => l.Id, l => l.UnitPrice);
        return rma.Lines.Sum(l => (unitPrices.TryGetValue(l.OrderLineItemId, out var up) ? up : 0m) * l.Quantity);
    }

    /// <summary>
    /// Builds a no-charge replacement order for the returned units, copying the original order's delivery
    /// target and fulfilling store. It enters the normal fulfilment lifecycle at Processing.
    /// </summary>
    private static Order CreateReplacementOrder(Order order, Domain.Entities.Rma rma, DateTime now)
    {
        var seq = rma.RmaNumber.Split('-').LastOrDefault() ?? "1";
        var replacement = new Order
        {
            OrderNumber = $"{order.OrderNumber}-R{seq}",
            CustomerId = order.CustomerId,
            Status = OrderStatus.Processing,
            PaymentStatus = PaymentStatus.Captured, // no-charge: the original order already paid
            ContactName = order.ContactName,
            ContactEmail = order.ContactEmail,
            Latitude = order.Latitude,
            Longitude = order.Longitude,
            CountryCode = order.CountryCode,
            Region = order.Region,
            PostalCode = order.PostalCode,
            AddressLine1 = order.AddressLine1,
            AddressLine2 = order.AddressLine2,
            City = order.City,
            StateProvince = order.StateProvince,
            AssignedStoreId = order.AssignedStoreId,
            IsPickup = order.IsPickup,
            CurrencyCode = order.CurrencyCode,
            Subtotal = 0m,
            DiscountTotal = 0m,
            ShippingTotal = 0m,
            TaxTotal = 0m,
            TotalAmount = 0m,
            PlacedOnUtc = now,
            RoutedOnUtc = order.AssignedStoreId is null ? null : now,
        };

        foreach (var line in rma.Lines)
        {
            replacement.Lines.Add(new OrderLineItem
            {
                ProductId = line.ProductId,
                ProductVariantId = line.ProductVariantId,
                ProductName = line.ProductName,
                Sku = line.Sku,
                Quantity = line.Quantity,
                UnitPrice = 0m,
                LineTotal = 0m,
            });
        }

        return replacement;
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
