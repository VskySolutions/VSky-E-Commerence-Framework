using VSky.Domain.Common;

namespace VSky.Domain.Entities;

/// <summary>
/// Shared shape of every per-integration credential row. Each concrete integration maps to its own table
/// (<c>Credentials_*</c>) so its fields are strongly typed rather than stored as generic key/value pairs.
/// A deployment may hold several rows per integration (e.g. a live and a sandbox account); the single row
/// with <see cref="Active"/> set is the one resolved at runtime. Secret fields are encrypted at rest via
/// the <see cref="EncryptedAttribute"/> value converter.
/// </summary>
public abstract class IntegrationCredentialBase : AuditableEntity, ISoftDeletable
{
    /// <summary>When set, this is the row used at runtime for its integration (at most one per integration).</summary>
    public bool Active { get; set; }

    /// <summary>Live (production) vs. sandbox/test credential — informational, surfaced in the admin list.</summary>
    public bool IsProduction { get; set; }

    /// <summary>Admin-facing label distinguishing rows of the same integration, e.g. "Live Stripe".</summary>
    public string Name { get; set; } = string.Empty;

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }
}

// ---- Payment gateways ------------------------------------------------------

/// <summary>Stripe API keys (table <c>Credentials_Stripe</c>).</summary>
public class StripeCredential : IntegrationCredentialBase
{
    public string? PublishableKey { get; set; }
    [Encrypted] public string? SecretKey { get; set; }
}

/// <summary>PayPal REST app credentials (table <c>Credentials_PayPal</c>).</summary>
public class PayPalCredential : IntegrationCredentialBase
{
    public string? ClientId { get; set; }
    [Encrypted] public string? SecretKey { get; set; }
}

/// <summary>Razorpay key id/secret (table <c>Credentials_Razorpay</c>).</summary>
public class RazorpayCredential : IntegrationCredentialBase
{
    public string? KeyId { get; set; }
    [Encrypted] public string? KeySecret { get; set; }
}

/// <summary>Square application credentials (table <c>Credentials_Square</c>).</summary>
public class SquareCredential : IntegrationCredentialBase
{
    public string? ApplicationId { get; set; }
    [Encrypted] public string? AccessToken { get; set; }
    [Encrypted] public string? ApplicationSecret { get; set; }
}

/// <summary>Authorize.Net merchant credentials (table <c>Credentials_AuthorizeNet</c>).</summary>
public class AuthorizeNetCredential : IntegrationCredentialBase
{
    public string? ApplicationLoginId { get; set; }
    [Encrypted] public string? TransactionKey { get; set; }
    [Encrypted] public string? SignatureKey { get; set; }
}

// ---- Tax providers ---------------------------------------------------------

/// <summary>TaxJar API token (table <c>Credentials_TaxJar</c>).</summary>
public class TaxJarCredential : IntegrationCredentialBase
{
    public string? BaseUrl { get; set; }
    [Encrypted] public string? SecretKey { get; set; }
}

/// <summary>Stripe Tax secret key (table <c>Credentials_StripeTax</c>).</summary>
public class StripeTaxCredential : IntegrationCredentialBase
{
    public string? PublishableKey { get; set; }
    [Encrypted] public string? SecretKey { get; set; }
}

// ---- Shipping carriers -----------------------------------------------------

/// <summary>FedEx API credentials (table <c>Credentials_FedEx</c>).</summary>
public class FedExCredential : IntegrationCredentialBase
{
    public string? ApiKey { get; set; }
    [Encrypted] public string? ApiSecret { get; set; }
}

/// <summary>DHL Express API credentials (table <c>Credentials_DHLExpress</c>).</summary>
public class DhlExpressCredential : IntegrationCredentialBase
{
    public string? ApiKey { get; set; }
    [Encrypted] public string? ApiSecret { get; set; }
}

/// <summary>USPS consumer key/secret (table <c>Credentials_USPS</c>).</summary>
public class UspsCredential : IntegrationCredentialBase
{
    public string? ConsumerKey { get; set; }
    [Encrypted] public string? ConsumerSecret { get; set; }
}

/// <summary>UPS OAuth2 client credentials + merchant/account number (table <c>Credentials_UPS</c>).</summary>
public class UpsCredential : IntegrationCredentialBase
{
    public string? MerchantId { get; set; }
    public string? ClientId { get; set; }
    [Encrypted] public string? ClientSecret { get; set; }
}

// ---- Communication ---------------------------------------------------------

/// <summary>Twilio account credentials (table <c>Credentials_Twilio</c>).</summary>
public class TwilioCredential : IntegrationCredentialBase
{
    public string? AccountSid { get; set; }
    [Encrypted] public string? AuthToken { get; set; }
    public string? WhatsAppNumber { get; set; }
}

// ---- Storage ---------------------------------------------------------------

/// <summary>Azure Blob storage connection string (table <c>Credentials_AzureBlob</c>).</summary>
public class AzureBlobCredential : IntegrationCredentialBase
{
    [Encrypted] public string? ConnectionString { get; set; }
}
