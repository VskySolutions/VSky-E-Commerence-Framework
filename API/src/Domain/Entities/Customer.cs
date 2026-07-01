using VSky.Domain.Common;

namespace VSky.Domain.Entities;

/// <summary>
/// Profile data linked 1:1 to a <see cref="User"/>. Holds all personal and preference data, kept
/// separate from authentication credentials (Database blueprint).
/// </summary>
public class Customer : AuditableEntity, ISoftDeletable
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsTaxExempt { get; set; }
    public string? TaxExemptionCertificate { get; set; }
    public string? VatId { get; set; }
    public string? WhatsAppPhoneNumber { get; set; }
    public bool WhatsAppOptIn { get; set; }
    public DateTime? WhatsAppOptInAtUtc { get; set; }

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    public User? User { get; set; }
}
