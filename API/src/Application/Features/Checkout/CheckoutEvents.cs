using MediatR;

namespace VSky.Application.Features.Checkout;

/// <summary>
/// Raised when a checkout completes with a successful payment (REQ-CHK-003 / AC-CHK-003.8). Downstream
/// handlers (order confirmation, analytics, fulfilment hand-off) subscribe to this rather than the
/// low-level routing/payment events.
/// </summary>
public record OrderPlaced(
    Guid OrderId,
    string OrderNumber,
    decimal Total,
    Guid? CustomerId,
    string? Email) : INotification;
