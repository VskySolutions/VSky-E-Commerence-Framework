using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;

namespace VSky.Infrastructure.Credentials;

/// <summary>
/// Column-level credential encryption backed by the .NET Data Protection API. The deployment-specific
/// key ring lives outside the main database (Credential Vault blueprint, encryption ADR).
/// </summary>
public class CredentialVault : ICredentialVault
{
    private readonly IDataProtector _protector;
    private readonly IApplicationDbContext _db;

    public CredentialVault(IDataProtectionProvider provider, IApplicationDbContext db)
    {
        _protector = provider.CreateProtector("VSky.CredentialVault.v1");
        _db = db;
    }

    public string Encrypt(string plaintext) => _protector.Protect(plaintext);

    public string Decrypt(string ciphertext) => _protector.Unprotect(ciphertext);

    public async Task<string?> GetCredentialAsync(string serviceType, CancellationToken cancellationToken = default)
    {
        var cred = await _db.TenantCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ServiceType == serviceType, cancellationToken);

        return cred is null ? null : Decrypt(cred.EncryptedValue);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetCredentialsAsync(string providerCode, CancellationToken cancellationToken = default)
    {
        // The IntegrationProvider query filter (soft-delete) applies through the navigation, so retired
        // providers resolve to nothing. Disabled providers are excluded explicitly.
        var fields = await _db.IntegrationCredentials
            .AsNoTracking()
            .Where(c => c.Provider!.Code == providerCode && c.Provider.IsEnabled)
            .Select(c => new { c.Definition!.FieldCode, c.Value, c.IsSecret })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in fields)
            result[f.FieldCode] = f.IsSecret ? Decrypt(f.Value) : f.Value;

        return result;
    }
}
