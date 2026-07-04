using VSky.Domain.Common;
using VSky.Domain.Enums;

namespace VSky.Domain.Entities;

/// <summary>
/// Top-level grouping of integration providers (Payment, Tax, Shipping, Communication, Storage).
/// Root of the dynamic four-table Credential Vault (WO-7): categories → providers → definitions →
/// values, so a new integration is added as data — no code or schema change (REQ-TEN-002).
/// </summary>
public class IntegrationCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Stable machine code (e.g. "payment").</summary>
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<IntegrationProvider> Providers { get; set; } = new List<IntegrationProvider>();
}

/// <summary>
/// A specific third-party provider (e.g. Stripe) within a category. Soft-deletable so a provider can be
/// retired without losing its stored-value audit history.
/// </summary>
public class IntegrationProvider : AuditableEntity, ISoftDeletable
{
    public Guid CategoryId { get; set; }
    public IntegrationCategory? Category { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Stable machine code (e.g. "stripe"); doubles as the runtime service-type lookup key.</summary>
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int DisplayOrder { get; set; }

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    public ICollection<CredentialDefinition> Definitions { get; set; } = new List<CredentialDefinition>();
    public ICollection<IntegrationCredential> Credentials { get; set; } = new List<IntegrationCredential>();
}

/// <summary>
/// Metadata for one credential field a provider requires. The admin credential form is generated entirely
/// from these rows (AC-TEN-002.4) — adding a provider field needs no code change.
/// </summary>
public class CredentialDefinition : AuditableEntity
{
    public Guid ProviderId { get; set; }
    public IntegrationProvider? Provider { get; set; }

    public string FieldName { get; set; } = string.Empty;

    /// <summary>Stable field code, used as the dictionary key when resolving credentials (e.g. "secret_key").</summary>
    public string FieldCode { get; set; } = string.Empty;
    public CredentialFieldType DataType { get; set; } = CredentialFieldType.String;
    public bool IsRequired { get; set; }

    /// <summary>Secret fields are encrypted at rest and masked (last 4 chars) in every read (AC-TEN-002.5/6).</summary>
    public bool IsSecret { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<IntegrationCredential> Values { get; set; } = new List<IntegrationCredential>();
}

/// <summary>
/// A stored value for one (provider, definition). Secret values hold ciphertext in <see cref="Value"/> and
/// the last four plaintext chars in <see cref="LastFourChars"/>; non-secret values are stored in plain text
/// (AC-TEN-002.5/6/7). <see cref="IsSecret"/> snapshots the definition flag so reads mask without a join.
/// </summary>
public class IntegrationCredential : AuditableEntity
{
    public Guid ProviderId { get; set; }
    public IntegrationProvider? Provider { get; set; }

    public Guid DefinitionId { get; set; }
    public CredentialDefinition? Definition { get; set; }

    public string Value { get; set; } = string.Empty;
    public string? LastFourChars { get; set; }
    public bool IsSecret { get; set; }
}
