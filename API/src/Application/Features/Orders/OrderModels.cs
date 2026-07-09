using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.Orders;

/// <summary>Full view of an order including its line items and (transiently) the routing chain.</summary>
public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? CountryCode { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Landmark { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public Guid? AssignedStoreId { get; set; }
    public DateTime PlacedOnUtc { get; set; }
    public DateTime? RoutedOnUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderLineItemDto> Lines { get; set; } = new();

    /// <summary>The routing evaluation chain — only populated on the response immediately after (re)routing.</summary>
    public List<StoreEvaluation>? RoutingChain { get; set; }

    public static OrderDto From(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        Status = o.Status.ToString(),
        CustomerId = o.CustomerId,
        ContactName = o.ContactName,
        ContactEmail = o.ContactEmail,
        ContactPhone = o.ContactPhone,
        Latitude = o.Latitude,
        Longitude = o.Longitude,
        CountryCode = o.CountryCode,
        Region = o.Region,
        PostalCode = o.PostalCode,
        AddressLine1 = o.AddressLine1,
        AddressLine2 = o.AddressLine2,
        Landmark = o.Landmark,
        City = o.City,
        StateProvince = o.StateProvince,
        AssignedStoreId = o.AssignedStoreId,
        PlacedOnUtc = o.PlacedOnUtc,
        RoutedOnUtc = o.RoutedOnUtc,
        TotalAmount = o.TotalAmount,
        Lines = o.Lines.Select(OrderLineItemDto.From).ToList(),
    };
}

/// <summary>A single order line with catalog values snapshotted at placement time.</summary>
public class OrderLineItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public static OrderLineItemDto From(OrderLineItem l) => new()
    {
        Id = l.Id,
        ProductId = l.ProductId,
        ProductVariantId = l.ProductVariantId,
        ProductName = l.ProductName,
        Sku = l.Sku,
        Quantity = l.Quantity,
        UnitPrice = l.UnitPrice,
        LineTotal = l.LineTotal,
    };
}

/// <summary>Condensed order row for the store order queue and the admin order list.</summary>
public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public DateTime PlacedOnUtc { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Number of distinct line items on the order.</summary>
    public int ItemCount { get; set; }

    public static OrderSummaryDto From(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        Status = o.Status.ToString(),
        ContactName = o.ContactName,
        PlacedOnUtc = o.PlacedOnUtc,
        TotalAmount = o.TotalAmount,
        ItemCount = o.Lines.Count,
    };
}
