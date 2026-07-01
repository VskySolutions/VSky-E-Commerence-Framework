using VSky.Domain.Entities;

namespace VSky.Application.Features.Stores;

/// <summary>Full configuration view of a store/fulfilment location.</summary>
public class StoreDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? OperatingHoursJson { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public string? CurrencyDisplay { get; set; }
    public bool IsEnabled { get; set; }
    public bool MaintenanceMode { get; set; }
    public bool GuestOrderingEnabled { get; set; }
    public string? DeliveryZoneJson { get; set; }
    public int? OrderCapacityLimit { get; set; }

    public static StoreDto From(Store s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        AddressLine1 = s.AddressLine1,
        AddressLine2 = s.AddressLine2,
        City = s.City,
        StateProvince = s.StateProvince,
        PostalCode = s.PostalCode,
        CountryCode = s.CountryCode,
        Latitude = s.Latitude,
        Longitude = s.Longitude,
        ContactEmail = s.ContactEmail,
        ContactPhone = s.ContactPhone,
        OperatingHoursJson = s.OperatingHoursJson,
        TimeZone = s.TimeZone,
        CurrencyDisplay = s.CurrencyDisplay,
        IsEnabled = s.IsEnabled,
        MaintenanceMode = s.MaintenanceMode,
        GuestOrderingEnabled = s.GuestOrderingEnabled,
        DeliveryZoneJson = s.DeliveryZoneJson,
        OrderCapacityLimit = s.OrderCapacityLimit,
    };
}
