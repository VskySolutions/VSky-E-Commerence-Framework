using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Infrastructure.Credentials;

/// <summary>
/// Runtime credential resolution over the per-integration <c>Credentials_*</c> tables, plus the generic
/// Encrypt/Decrypt used by the few secrets stored elsewhere (SMTP, reCAPTCHA). Integration adapters keep
/// calling <see cref="GetCredentialAsync"/> with their service-type key; this reads the active row of the
/// matching table and rebuilds the exact string/JSON that adapter parses. Secret columns are decrypted
/// transparently by the EF value converter, so this only assembles the resolved shape.
/// </summary>
public class CredentialVault : ICredentialVault
{
    private readonly ICredentialProtector _protector;
    private readonly IApplicationDbContext _db;

    public CredentialVault(ICredentialProtector protector, IApplicationDbContext db)
    {
        _protector = protector;
        _db = db;
    }

    public string Encrypt(string plaintext) => _protector.Protect(plaintext);

    public string Decrypt(string ciphertext) => _protector.Unprotect(ciphertext);

    public async Task<string?> GetCredentialAsync(string serviceType, CancellationToken cancellationToken = default)
        => (await GetResolvedCredentialAsync(serviceType, cancellationToken))?.Value;

    public async Task<ResolvedCredential?> GetResolvedCredentialAsync(string serviceType, CancellationToken cancellationToken = default)
    {
        return serviceType switch
        {
            // Payment gateways
            "stripe"       => Resolve(await ActiveAsync(_db.StripeCredentials, cancellationToken), c => Json(new { secretKey = c.SecretKey, returnUrl = c.ReturnUrl })),
            "paypal"       => Resolve(await ActiveAsync(_db.PayPalCredentials, cancellationToken), c => Pair(c.ClientId, c.SecretKey)),
            "razorpay"     => Resolve(await ActiveAsync(_db.RazorpayCredentials, cancellationToken), c => Pair(c.KeyId, c.KeySecret)),
            "square"       => Resolve(await ActiveAsync(_db.SquareCredentials, cancellationToken), c => c.AccessToken),
            "authorizenet" => Resolve(await ActiveAsync(_db.AuthorizeNetCredentials, cancellationToken), c => Pair(c.ApplicationLoginId, c.TransactionKey)),
            // Tax providers
            "taxjar"       => Resolve(await ActiveAsync(_db.TaxJarCredentials, cancellationToken), c => c.SecretKey),
            "stripe-tax"   => Resolve(await ActiveAsync(_db.StripeTaxCredentials, cancellationToken), c => c.SecretKey),
            // Storage
            "azure-blob"   => Resolve(await ActiveAsync(_db.AzureBlobCredentials, cancellationToken), c => c.ConnectionString),
            // Shipping carriers (adapters parse a small JSON document)
            "fedex"        => Resolve(await ActiveAsync(_db.FedExCredentials, cancellationToken), c => Json(new { apiKey = c.ApiKey, secretKey = c.ApiSecret })),
            "dhl"          => Resolve(await ActiveAsync(_db.DhlExpressCredentials, cancellationToken), c => Json(new { apiKey = c.ApiKey, apiSecret = c.ApiSecret })),
            "usps"         => Resolve(await ActiveAsync(_db.UspsCredentials, cancellationToken), c => Json(new { consumerKey = c.ConsumerKey, consumerSecret = c.ConsumerSecret })),
            "ups"          => Resolve(await ActiveAsync(_db.UpsCredentials, cancellationToken), c => Json(new { clientId = c.ClientId, clientSecret = c.ClientSecret, merchantId = c.MerchantId })),
            _              => null,
        };
    }

    /// <summary>The active (non-deleted) row for an integration, most-recently-updated first.</summary>
    private static Task<T?> ActiveAsync<T>(DbSet<T> set, CancellationToken ct) where T : IntegrationCredentialBase
        => set.AsNoTracking()
            .Where(x => x.Active)
            .OrderByDescending(x => x.UpdatedOnUtc)
            .FirstOrDefaultAsync(ct);

    /// <summary>Projects the active row's credential string and pairs it with its production flag; null when unusable.</summary>
    private static ResolvedCredential? Resolve<T>(T? credential, Func<T, string?> project) where T : IntegrationCredentialBase
    {
        if (credential is null) return null;
        var value = project(credential);
        return string.IsNullOrWhiteSpace(value) ? null : new ResolvedCredential(value, credential.IsProduction);
    }

    /// <summary>Colon-joined "id:secret" pair; null unless both halves are present.</summary>
    private static string? Pair(string? left, string? right)
        => string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right) ? null : $"{left}:{right}";

    /// <summary>Serializes the projected shape (using its runtime type so its members are emitted).</summary>
    private static string Json(object shape) => JsonSerializer.Serialize(shape, shape.GetType());
}
