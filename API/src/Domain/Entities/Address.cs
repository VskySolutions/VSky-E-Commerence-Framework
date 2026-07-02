using VSky.Domain.Common;
using VSky.Domain.Enums;

namespace VSky.Domain.Entities;

/// <summary>
/// A saved shipping or billing address in a customer's address book (REQ-CUS-002). At most one
/// address per <see cref="AddressType"/> per customer may be the default (enforced by the handler
/// and a filtered unique index).
/// </summary>
public class Address : AuditableEntity, ISoftDeletable
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public AddressType AddressType { get; set; }
    public bool IsDefault { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? StateProvince { get; set; }
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>ISO 3166-1 alpha-2 country code.</summary>
    public string CountryCode { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }
}
