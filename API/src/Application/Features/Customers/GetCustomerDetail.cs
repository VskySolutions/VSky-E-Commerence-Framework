using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Features.CustomerAddresses;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Customers;

/// <summary>A customer role the customer belongs to, summarized for the detail view (WO-117).</summary>
public class CustomerRoleBriefDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>One of a customer's orders, summarized for the detail view's order history (WO-117).</summary>
public class CustomerOrderSummaryDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime PlacedOnUtc { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Full admin view of a single customer (WO-117): profile, tax-exemption, assigned customer roles,
/// saved address book and a summary of their order history (lifetime spend + most recent orders).
/// </summary>
public class CustomerDetailDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedOnUtc { get; set; }

    public bool IsTaxExempt { get; set; }
    public string? TaxExemptionCertificate { get; set; }
    public string? VatId { get; set; }

    public string? WhatsAppPhoneNumber { get; set; }
    public bool WhatsAppOptIn { get; set; }

    public List<CustomerRoleBriefDto> Roles { get; set; } = new();
    public List<AddressDto> Addresses { get; set; } = new();

    public int OrderCount { get; set; }
    /// <summary>Lifetime value: the total of orders whose payment was captured (net of fully-refunded ones).</summary>
    public decimal TotalSpent { get; set; }
    public List<CustomerOrderSummaryDto> RecentOrders { get; set; } = new();
}

/// <summary>Loads the full admin detail for one customer (WO-117).</summary>
public record GetCustomerDetailQuery(Guid Id) : IRequest<CustomerDetailDto>;

public class GetCustomerDetailQueryHandler : IRequestHandler<GetCustomerDetailQuery, CustomerDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetCustomerDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CustomerDetailDto> Handle(GetCustomerDetailQuery request, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers.AsNoTracking()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        var roles = await _db.CustomerRoleAssignments.AsNoTracking()
            .Where(a => a.CustomerId == request.Id)
            .Join(_db.CustomerRoles, a => a.CustomerRoleId, r => r.Id,
                (a, r) => new CustomerRoleBriefDto { Id = r.Id, Name = r.Name })
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var addresses = await _db.Addresses.AsNoTracking()
            .Where(a => a.CustomerId == request.Id)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.AddressType)
            .ToListAsync(cancellationToken);

        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.CustomerId == request.Id)
            .OrderByDescending(o => o.PlacedOnUtc)
            .ToListAsync(cancellationToken);

        return new CustomerDetailDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.User?.Email ?? string.Empty,
            PhoneNumber = customer.PhoneNumber,
            EmailVerified = customer.User?.EmailVerified ?? false,
            CreatedOnUtc = customer.CreatedOnUtc,
            IsTaxExempt = customer.IsTaxExempt,
            TaxExemptionCertificate = customer.TaxExemptionCertificate,
            VatId = customer.VatId,
            WhatsAppPhoneNumber = customer.WhatsAppPhoneNumber,
            WhatsAppOptIn = customer.WhatsAppOptIn,
            Roles = roles,
            Addresses = addresses.Select(AddressDto.From).ToList(),
            OrderCount = orders.Count,
            TotalSpent = orders
                .Where(o => o.PaymentStatus is PaymentStatus.Captured or PaymentStatus.PartiallyRefunded)
                .Sum(o => o.TotalAmount),
            RecentOrders = orders.Take(10).Select(o => new CustomerOrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                PlacedOnUtc = o.PlacedOnUtc,
                CurrencyCode = o.CurrencyCode,
                TotalAmount = o.TotalAmount,
            }).ToList(),
        };
    }
}
