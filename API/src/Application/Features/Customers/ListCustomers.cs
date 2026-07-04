using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

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

/// <summary>Lists customers (newest first), optionally filtered by name or email (WO-117).</summary>
public record ListCustomersQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PaginatedList<CustomerListItemDto>>;

public class ListCustomersQueryHandler : IRequestHandler<ListCustomersQuery, PaginatedList<CustomerListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public ListCustomersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<CustomerListItemDto>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Customers.AsNoTracking().Include(c => c.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c =>
                c.FirstName.Contains(term)
                || c.LastName.Contains(term)
                || (c.User != null && c.User.Email.Contains(term)));
        }

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
