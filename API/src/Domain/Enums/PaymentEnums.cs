namespace VSky.Domain.Enums;

/// <summary>Lifecycle of a payment record (REQ-PAY-001/002/003).</summary>
public enum PaymentStatus
{
    Pending = 0,
    Authorized = 1,
    Captured = 2,
    Failed = 3,
    Refunded = 4,
    PartiallyRefunded = 5,
    Voided = 6,
    AwaitingPayment = 7
}

/// <summary>Supported payment methods (AC-PAY-001.4/001.5).</summary>
public enum PaymentMethodType
{
    Stripe = 0,
    PayPal = 1,
    Razorpay = 2,
    Square = 3,
    AuthorizeNet = 4,
    CashOnDelivery = 5,
    BankTransfer = 6
}

/// <summary>Per-gateway capture mode (REQ-PAY-002).</summary>
public enum CaptureMode
{
    AuthorizeAndCapture = 0,
    AuthorizeOnly = 1
}
