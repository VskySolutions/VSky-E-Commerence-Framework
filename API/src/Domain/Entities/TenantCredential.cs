using VSky.Domain.Common;

namespace VSky.Domain.Entities;

/// <summary>
/// A third-party credential encrypted at the column level via the .NET Data Protection API
/// (Credential Vault / Database blueprints). Only <see cref="LastFourChars"/> is ever exposed.
/// Keyed by <see cref="ServiceType"/> (e.g. "stripe", "smtp", "azure-blob").
/// </summary>
public class TenantCredential : BaseEntity
{
    public string ServiceType { get; set; } = string.Empty;
    public string EncryptedValue { get; set; } = string.Empty;
    public string LastFourChars { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
}
