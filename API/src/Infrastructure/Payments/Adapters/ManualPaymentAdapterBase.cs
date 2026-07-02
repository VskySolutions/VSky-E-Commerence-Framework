using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Infrastructure.Payments.Adapters;

/// <summary>
/// Shared behavior for manual/offline payment methods (WO-34, AC-PAY-001.5): no external gateway call.
/// Authorization leaves the order awaiting out-of-band payment; capture and refund are admin actions that
/// simply advance the local record. A deterministic local reference is generated from the order id.
/// </summary>
public abstract class ManualPaymentAdapterBase : IPaymentGatewayAdapter
{
    public abstract PaymentMethodType Method { get; }

    /// <summary>Prefix for the generated local reference (e.g. "COD", "BANK").</summary>
    protected abstract string ReferencePrefix { get; }

    public Task<PaymentResult> AuthorizeAsync(PaymentRequest req, CaptureMode mode, CancellationToken ct)
    {
        var reference = BuildReference(req.OrderId);
        // Funds settle out-of-band, so the order simply awaits payment — no hold, no capture yet.
        return Task.FromResult(new PaymentResult(
            true, PaymentStatus.AwaitingPayment, AuthorizationId: reference, GatewayReference: reference));
    }

    public Task<PaymentResult> CaptureAsync(PaymentRecord payment, decimal amount, CancellationToken ct)
    {
        var reference = payment.GatewayReference ?? payment.AuthorizationId ?? BuildReference(payment.OrderId);
        return Task.FromResult(PaymentResult.Captured(reference, amount, reference, reference));
    }

    public Task<PaymentResult> RefundAsync(PaymentRecord payment, decimal amount, CancellationToken ct)
    {
        var reference = payment.GatewayReference ?? payment.AuthorizationId ?? BuildReference(payment.OrderId);
        return Task.FromResult(new PaymentResult(
            true, PaymentStatus.Refunded, TransactionId: reference, GatewayReference: reference, CapturedAmount: amount));
    }

    private string BuildReference(Guid orderId) => $"{ReferencePrefix}-{orderId:N}";
}
