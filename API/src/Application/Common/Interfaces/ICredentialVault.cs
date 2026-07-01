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
}
