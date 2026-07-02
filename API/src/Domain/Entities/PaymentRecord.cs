using VSky.Domain.Common;
using VSky.Domain.Enums;

namespace VSky.Domain.Entities;

/// <summary>
/// A payment attempt/record against an <see cref="Order"/>, tracking the full authorize → capture →
/// refund lifecycle (REQ-PAY-001/002/003). Gateway references are stored for later capture/refund.
/// </summary>
public class PaymentRecord : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public PaymentMethodType Method { get; set; }
    public string? GatewayName { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal RefundedAmount { get; set; }

    public string? AuthorizationId { get; set; }
    public string? TransactionId { get; set; }
    public string? GatewayReference { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime? AuthorizedOnUtc { get; set; }
    public DateTime? CapturedOnUtc { get; set; }
    public DateTime? RefundedOnUtc { get; set; }

    /// <summary>When an authorization-only hold expires; used to flag orders for manual review (AC-PAY-002.4).</summary>
    public DateTime? AuthorizationExpiresUtc { get; set; }
}
