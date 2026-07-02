using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CustomerAddresses;

/// <summary>A single saved shipping or billing address in a customer's address book (REQ-CUS-002).</summary>
public class AddressDto
{
    public Guid Id { get; set; }
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
    public string CountryCode { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public static AddressDto From(Address a) => new()
    {
        Id = a.Id,
        AddressType = a.AddressType,
        IsDefault = a.IsDefault,
        FirstName = a.FirstName,
        LastName = a.LastName,
        Company = a.Company,
        AddressLine1 = a.AddressLine1,
        AddressLine2 = a.AddressLine2,
        City = a.City,
        StateProvince = a.StateProvince,
        PostalCode = a.PostalCode,
        CountryCode = a.CountryCode,
        PhoneNumber = a.PhoneNumber,
    };
}

/// <summary>
/// A customer's addresses grouped by type, for checkout address selection (AC-CUS-002.2). Within each
/// group the default address (if any) is listed first.
/// </summary>
public class AddressBookDto
{
    public List<AddressDto> Shipping { get; set; } = new();
    public List<AddressDto> Billing { get; set; } = new();
}
