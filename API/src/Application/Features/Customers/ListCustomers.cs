using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Customers;

/// <summary>A customer row for the admin customer-management list (WO-117).</summary>
public class CustomerListItemDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}

/// <summary>Lists customers (newest first), optionally filtered by name/email search, email-verified, account-active and/or tax-exempt state (WO-117).</summary>
public record ListCustomersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? EmailVerified = null,
    bool? IsActive = null,
    bool? IsTaxExempt = null) : IRequest<PaginatedList<CustomerListItemDto>>;

public class ListCustomersQueryHandler : IRequestHandler<ListCustomersQuery, PaginatedList<CustomerListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public ListCustomersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<CustomerListItemDto>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        // Only genuine customers: a customer is a User carrying the Customer system role (assigned on
        // registration and mapped for pre-existing accounts). Staff carry admin roles instead — and they
        // also get a Customer profile at creation, so filtering by the Customer role is what excludes them.
        var customerRole = nameof(RoleType.Customer);
        var query = _db.Customers.AsNoTracking().Include(c => c.User)
            .Where(c => c.User != null && c.User.UserRoles.Any(ur => ur.Role!.Name == customerRole))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c =>
                c.FirstName.Contains(term)
                || c.LastName.Contains(term)
                || (c.User != null && c.User.Email.Contains(term)));
        }

        if (request.EmailVerified.HasValue)
            query = query.Where(c => c.User != null && c.User.EmailVerified == request.EmailVerified.Value);

        if (request.IsActive.HasValue)
            query = query.Where(c => c.User != null && c.User.IsActive == request.IsActive.Value);

        if (request.IsTaxExempt.HasValue)
            query = query.Where(c => c.IsTaxExempt == request.IsTaxExempt.Value);

        var ordered = query.OrderByDescending(c => c.CreatedOnUtc);
        var page = await PaginatedList<Customer>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);

        var items = page.Items.Select(c => new CustomerListItemDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.User?.Email ?? string.Empty,
            PhoneNumber = c.PhoneNumber,
            EmailVerified = c.User?.EmailVerified ?? false,
            CreatedOnUtc = c.CreatedOnUtc,
        }).ToList();

        return new PaginatedList<CustomerListItemDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
