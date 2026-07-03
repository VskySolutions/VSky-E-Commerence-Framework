using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.CustomerRoles;

// ---- Assign customer ↔ roles -------------------------------------------------

/// <summary>Replaces a customer's role assignments (AC-CUS-003.2).</summary>
public record AssignCustomerRolesCommand(Guid CustomerId, List<Guid> RoleIds) : IRequest<List<CustomerRoleDto>>;

public class AssignCustomerRolesCommandHandler : IRequestHandler<AssignCustomerRolesCommand, List<CustomerRoleDto>>
{
    private readonly IApplicationDbContext _db;
    public AssignCustomerRolesCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CustomerRoleDto>> Handle(AssignCustomerRolesCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken))
            throw new NotFoundException(nameof(Customer), request.CustomerId);

        var roleIds = (request.RoleIds ?? new List<Guid>()).Distinct().ToList();
        if (roleIds.Count > 0)
        {
            var found = await _db.CustomerRoles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Id).ToListAsync(cancellationToken);
            var missing = roleIds.Except(found).ToList();
            if (missing.Count > 0)
                throw new NotFoundException(nameof(CustomerRole), missing[0]);
        }

        var existing = await _db.CustomerRoleAssignments.Where(a => a.CustomerId == request.CustomerId).ToListAsync(cancellationToken);
        foreach (var assignment in existing.Where(a => !roleIds.Contains(a.CustomerRoleId)))
            _db.CustomerRoleAssignments.Remove(assignment);

        var assigned = existing.Select(a => a.CustomerRoleId).ToHashSet();
        foreach (var roleId in roleIds.Where(id => !assigned.Contains(id)))
            _db.CustomerRoleAssignments.Add(new CustomerRoleAssignment { CustomerId = request.CustomerId, CustomerRoleId = roleId });

        await _db.SaveChangesAsync(cancellationToken);
        return await LoadAsync(request.CustomerId, cancellationToken);
    }

    private async Task<List<CustomerRoleDto>> LoadAsync(Guid customerId, CancellationToken ct)
    {
        var roles = await _db.CustomerRoleAssignments.AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .Join(_db.CustomerRoles.Include(r => r.GroupPrices), a => a.CustomerRoleId, r => r.Id, (a, r) => r)
            .ToListAsync(ct);
        return roles.Select(CustomerRoleDto.From).ToList();
    }
}

/// <summary>Lists a customer's assigned roles.</summary>
public record GetCustomerRolesQuery(Guid CustomerId) : IRequest<List<CustomerRoleDto>>;

public class GetCustomerRolesQueryHandler : IRequestHandler<GetCustomerRolesQuery, List<CustomerRoleDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCustomerRolesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CustomerRoleDto>> Handle(GetCustomerRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _db.CustomerRoleAssignments.AsNoTracking()
            .Where(a => a.CustomerId == request.CustomerId)
            .Join(_db.CustomerRoles.Include(r => r.GroupPrices), a => a.CustomerRoleId, r => r.Id, (a, r) => r)
            .ToListAsync(cancellationToken);
        return roles.Select(CustomerRoleDto.From).ToList();
    }
}

// ---- Access restrictions -----------------------------------------------------

/// <summary>Replaces the roles allowed to see a product; empty = visible to all (AC-CUS-003.4).</summary>
public record SetProductRoleRestrictionsCommand(Guid ProductId, List<Guid> RoleIds) : IRequest;

public class SetProductRoleRestrictionsCommandHandler : IRequestHandler<SetProductRoleRestrictionsCommand>
{
    private readonly IApplicationDbContext _db;
    public SetProductRoleRestrictionsCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(SetProductRoleRestrictionsCommand request, CancellationToken cancellationToken)
    {
        var roleIds = (request.RoleIds ?? new List<Guid>()).Distinct().ToList();
        var existing = await _db.ProductRoleRestrictions.Where(r => r.ProductId == request.ProductId).ToListAsync(cancellationToken);

        foreach (var restriction in existing.Where(r => !roleIds.Contains(r.CustomerRoleId)))
            _db.ProductRoleRestrictions.Remove(restriction);

        var have = existing.Select(r => r.CustomerRoleId).ToHashSet();
        foreach (var roleId in roleIds.Where(id => !have.Contains(id)))
            _db.ProductRoleRestrictions.Add(new ProductRoleRestriction { ProductId = request.ProductId, CustomerRoleId = roleId });

        await _db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Replaces the roles allowed to see a category; empty = visible to all (AC-CUS-003.4).</summary>
public record SetCategoryRoleRestrictionsCommand(Guid CategoryId, List<Guid> RoleIds) : IRequest;

public class SetCategoryRoleRestrictionsCommandHandler : IRequestHandler<SetCategoryRoleRestrictionsCommand>
{
    private readonly IApplicationDbContext _db;
    public SetCategoryRoleRestrictionsCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(SetCategoryRoleRestrictionsCommand request, CancellationToken cancellationToken)
    {
        var roleIds = (request.RoleIds ?? new List<Guid>()).Distinct().ToList();
        var existing = await _db.CategoryRoleRestrictions.Where(r => r.CategoryId == request.CategoryId).ToListAsync(cancellationToken);

        foreach (var restriction in existing.Where(r => !roleIds.Contains(r.CustomerRoleId)))
            _db.CategoryRoleRestrictions.Remove(restriction);

        var have = existing.Select(r => r.CustomerRoleId).ToHashSet();
        foreach (var roleId in roleIds.Where(id => !have.Contains(id)))
            _db.CategoryRoleRestrictions.Add(new CategoryRoleRestriction { CategoryId = request.CategoryId, CustomerRoleId = roleId });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
