using VSky.Domain.Enums;

namespace VSky.Infrastructure.Persistence.Seed;

/// <summary>
/// Canonical integration catalogue seeded into the dynamic Credential Vault (WO-7): categories, providers,
/// and each provider's credential field definitions. New providers are added here as data — no schema or
/// service change (REQ-TEN-002). Provider codes double as the runtime service-type lookup key.
/// </summary>
public static class DefaultIntegrationProviders
{
    public sealed record FieldSeed(
        string FieldName,
        string FieldCode,
        bool Required,
        bool Secret,
        string? Placeholder = null,
        string? HelpText = null,
        CredentialFieldType DataType = CredentialFieldType.String);

    public sealed record ProviderSeed(string Name, string Code, string? Description, FieldSeed[] Fields);

    public sealed record CategorySeed(string Name, string Code, string? Description, ProviderSeed[] Providers);

    public static readonly CategorySeed[] Catalogue =
    {
        new("Payment", "payment", "Payment gateways that authorize, capture, and refund transactions.", new[]
        {
            new ProviderSeed("Stripe", "stripe", "Cards, wallets, and more via Stripe.", new[]
            {
                new FieldSeed("Secret Key", "secret_key", true, true, "sk_live_…", "Your Stripe secret API key."),
                new FieldSeed("Publishable Key", "publishable_key", false, false, "pk_live_…", "Used by the storefront to tokenize cards."),
                new FieldSeed("Webhook Signing Secret", "webhook_secret", false, true, "whsec_…", "Verifies incoming Stripe webhook signatures."),
            }),
            new ProviderSeed("PayPal", "paypal", "PayPal Checkout.", new[]
            {
                new FieldSeed("Client ID", "client_id", true, false, null, "REST app client ID."),
                new FieldSeed("Client Secret", "client_secret", true, true, null, "REST app secret."),
                new FieldSeed("Environment", "environment", true, false, "live", "\"live\" or \"sandbox\"."),
            }),
            new ProviderSeed("Razorpay", "razorpay", "Razorpay payments.", new[]
            {
                new FieldSeed("Key ID", "key_id", true, false, "rzp_live_…"),
                new FieldSeed("Key Secret", "key_secret", true, true),
            }),
            new ProviderSeed("Square", "square", "Square payments.", new[]
            {
                new FieldSeed("Access Token", "access_token", true, true),
                new FieldSeed("Application ID", "application_id", false, false),
                new FieldSeed("Location ID", "location_id", false, false),
            }),
            new ProviderSeed("Authorize.NET", "authorizenet", "Authorize.Net payment gateway.", new[]
            {
                new FieldSeed("API Login ID", "api_login_id", true, false),
                new FieldSeed("Transaction Key", "transaction_key", true, true),
            }),
        }),
        new("Tax", "tax", "Sales-tax calculation providers.", new[]
        {
            new ProviderSeed("TaxJar", "taxjar", "TaxJar sales-tax API.", new[]
            {
                new FieldSeed("API Token", "api_token", true, true),
            }),
            new ProviderSeed("Stripe Tax", "stripe-tax", "Stripe Tax calculation.", new[]
            {
                new FieldSeed("Secret Key", "secret_key", true, true, "sk_live_…"),
            }),
        }),
        new("Shipping", "shipping", "Carrier rate, label, and tracking providers.", new[]
        {
            new ProviderSeed("DHL Express", "dhl", "DHL Express rates and labels.", new[]
            {
                new FieldSeed("API Key", "api_key", true, true),
                new FieldSeed("API Secret", "api_secret", true, true),
                new FieldSeed("Account Number", "account_number", false, false),
            }),
            new ProviderSeed("UPS", "ups", "UPS shipping.", new[]
            {
                new FieldSeed("Client ID", "client_id", true, false),
                new FieldSeed("Client Secret", "client_secret", true, true),
                new FieldSeed("Account Number", "account_number", false, false),
            }),
            new ProviderSeed("FedEx", "fedex", "FedEx shipping.", new[]
            {
                new FieldSeed("API Key", "api_key", true, false),
                new FieldSeed("Secret Key", "secret_key", true, true),
                new FieldSeed("Account Number", "account_number", false, false),
            }),
            new ProviderSeed("USPS", "usps", "USPS shipping.", new[]
            {
                new FieldSeed("Consumer Key", "consumer_key", true, true),
                new FieldSeed("Consumer Secret", "consumer_secret", true, true),
            }),
        }),
        new("Communication", "communication", "SMS and messaging providers.", new[]
        {
            new ProviderSeed("Twilio", "twilio", "Twilio SMS.", new[]
            {
                new FieldSeed("Account SID", "account_sid", true, false, "AC…"),
                new FieldSeed("Auth Token", "auth_token", true, true),
                new FieldSeed("From Number", "from_number", false, false, "+1…", "Default sender number in E.164 format."),
            }),
        }),
        new("Storage", "storage", "File and object storage providers.", new[]
        {
            new ProviderSeed("Azure Blob Storage", "azure-blob", "Azure Blob object storage.", new[]
            {
                new FieldSeed("Connection String", "connection_string", true, true, null, "Storage account connection string."),
                new FieldSeed("Container Name", "container_name", false, false, "uploads"),
            }),
        }),
    };
}
