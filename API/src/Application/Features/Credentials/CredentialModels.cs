using VSky.Domain.Entities;

namespace VSky.Application.Features.Credentials;

/// <summary>Masked view of a stored credential. Only the last four characters are ever exposed.</summary>
public class CredentialSummaryDto
{
    private const string Mask = "••••"; // ••••

    public string ServiceType { get; set; } = string.Empty;
    public string? MaskedValue { get; set; }
    public string? Description { get; set; }
    public bool IsConfigured { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public static CredentialSummaryDto From(TenantCredential c) => new()
    {
        ServiceType = c.ServiceType,
        MaskedValue = string.IsNullOrEmpty(c.LastFourChars) ? null : Mask + c.LastFourChars,
        Description = c.Description,
        IsConfigured = !string.IsNullOrEmpty(c.EncryptedValue),
        UpdatedAtUtc = c.UpdatedOnUtc,
    };
}
