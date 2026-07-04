namespace VSky.Application.Common.Interfaces;

/// <summary>
/// Encrypts/decrypts third-party credentials via the .NET Data Protection API and resolves them at
/// runtime for integration clients. Plaintext is held only transiently (Credential Vault blueprint).
/// </summary>
public interface ICredentialVault
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    Task<string?> GetCredentialAsync(string serviceType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves every stored field for a provider (by its code) as a field-code → plaintext dictionary,
    /// decrypting secret fields. This is the centralised read path for the dynamic Credential Vault
    /// (AC-TEN-002.10); returns an empty dictionary if the provider is unknown, disabled, or unconfigured.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetCredentialsAsync(string providerCode, CancellationToken cancellationToken = default);
}
