using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;

namespace VSky.Application.Features.AdminUsers;

/// <summary>Returns a page of users ordered by email, optionally filtered by an email/username search term.</summary>
public record ListUsersQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PaginatedList<AdminUserDto>>;

public class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, PaginatedList<AdminUserDto>>
{
    private readonly IApplicationDbContext _db;

    public ListUsersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PaginatedList<AdminUserDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        IQueryable<User> query = _db.Users
            .AsNoTracking()
            .Include(u => u.Customer)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(u => u.Email.Contains(term) || u.Username.Contains(term));
        }

        var ordered = query.OrderBy(u => u.Email);

        var page = await PaginatedList<User>.CreateAsync(ordered, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(AdminUserDto.From).ToList();
        return new PaginatedList<AdminUserDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }
}
