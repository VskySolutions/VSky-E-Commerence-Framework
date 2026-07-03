using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CustomerRoles;

// ---- Queries -----------------------------------------------------------------

/// <summary>Lists customer roles (with group prices), ordered for display.</summary>
public record ListCustomerRolesQuery : IRequest<List<CustomerRoleDto>>;

public class ListCustomerRolesQueryHandler : IRequestHandler<ListCustomerRolesQuery, List<CustomerRoleDto>>
{
    private readonly IApplicationDbContext _db;
    public ListCustomerRolesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CustomerRoleDto>> Handle(ListCustomerRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _db.CustomerRoles.AsNoTracking().Include(r => r.GroupPrices)
            .OrderBy(r => r.DisplayOrder).ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
        return roles.Select(CustomerRoleDto.From).ToList();
    }
}

/// <summary>Gets one customer role.</summary>
public record GetCustomerRoleQuery(Guid Id) : IRequest<CustomerRoleDto>;

public class GetCustomerRoleQueryHandler : IRequestHandler<GetCustomerRoleQuery, CustomerRoleDto>
{
    private readonly IApplicationDbContext _db;
    public GetCustomerRoleQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CustomerRoleDto> Handle(GetCustomerRoleQuery request, CancellationToken cancellationToken)
    {
        var role = await _db.CustomerRoles.AsNoTracking().Include(r => r.GroupPrices)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerRole), request.Id);
        return CustomerRoleDto.From(role);
    }
}

// ---- Commands ----------------------------------------------------------------

/// <summary>Creates a customer role with a pricing rule (AC-CUS-003.1).</summary>
public record CreateCustomerRoleCommand(
    string Name, string? Description, CustomerRolePricingRuleType PricingRuleType,
    decimal? DiscountPercent = null, bool IsActive = true, int DisplayOrder = 0) : IRequest<CustomerRoleDto>;

public class CreateCustomerRoleCommandValidator : AbstractValidator<CreateCustomerRoleCommand>
{
    public CreateCustomerRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100)
            .When(x => x.PricingRuleType == CustomerRolePricingRuleType.PercentageDiscount);
    }
}

public class CreateCustomerRoleCommandHandler : IRequestHandler<CreateCustomerRoleCommand, CustomerRoleDto>
{
    private readonly IApplicationDbContext _db;
    public CreateCustomerRoleCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<CustomerRoleDto> Handle(CreateCustomerRoleCommand request, CancellationToken cancellationToken)
    {
        var role = new CustomerRole
        {
            Name = request.Name,
            Description = request.Description,
            PricingRuleType = request.PricingRuleType,
            DiscountPercent = request.PricingRuleType == CustomerRolePricingRuleType.PercentageDiscount ? request.DiscountPercent : null,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
        };
        _db.CustomerRoles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);
        return CustomerRoleDto.From(role);
    }
}

/// <summary>Updates a customer role.</summary>
public record UpdateCustomerRoleCommand(
    Guid Id, string Name, string? Description, CustomerRolePricingRuleType PricingRuleType,
    decimal? DiscountPercent, bool IsActive, int DisplayOrder) : IRequest<CustomerRoleDto>;

public class UpdateCustomerRoleCommandHandler : IRequestHandler<UpdateCustomerRoleCommand, CustomerRoleDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateCustomerRoleCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<CustomerRoleDto> Handle(UpdateCustomerRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _db.CustomerRoles.Include(r => r.GroupPrices).FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerRole), request.Id);

        role.Name = request.Name;
        role.Description = request.Description;
        role.PricingRuleType = request.PricingRuleType;
        role.DiscountPercent = request.PricingRuleType == CustomerRolePricingRuleType.PercentageDiscount ? request.DiscountPercent : null;
        role.IsActive = request.IsActive;
        role.DisplayOrder = request.DisplayOrder;
        await _db.SaveChangesAsync(cancellationToken);
        return CustomerRoleDto.From(role);
    }
}

/// <summary>Removes a customer role (soft delete).</summary>
public record DeleteCustomerRoleCommand(Guid Id) : IRequest;

public class DeleteCustomerRoleCommandHandler : IRequestHandler<DeleteCustomerRoleCommand>
{
    private readonly IApplicationDbContext _db;
    public DeleteCustomerRoleCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCustomerRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _db.CustomerRoles.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (role is null)
            return;
        _db.CustomerRoles.Remove(role);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Replaces the group prices of a role (AC-CUS-003.1).</summary>
public record SetGroupPricesCommand(Guid RoleId, List<CustomerGroupPriceInput> Prices) : IRequest<CustomerRoleDto>;

public class SetGroupPricesCommandHandler : IRequestHandler<SetGroupPricesCommand, CustomerRoleDto>
{
    private readonly IApplicationDbContext _db;
    public SetGroupPricesCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<CustomerRoleDto> Handle(SetGroupPricesCommand request, CancellationToken cancellationToken)
    {
        var role = await _db.CustomerRoles.Include(r => r.GroupPrices).FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerRole), request.RoleId);

        foreach (var existing in role.GroupPrices.ToList())
            _db.CustomerGroupPrices.Remove(existing);

        foreach (var input in request.Prices ?? new List<CustomerGroupPriceInput>())
            _db.CustomerGroupPrices.Add(new CustomerGroupPrice
            {
                CustomerRoleId = role.Id,
                ProductId = input.ProductId,
                ProductVariantId = input.ProductVariantId,
                Price = input.Price,
            });

        await _db.SaveChangesAsync(cancellationToken);
        var reloaded = await _db.CustomerRoles.AsNoTracking().Include(r => r.GroupPrices).FirstAsync(r => r.Id == role.Id, cancellationToken);
        return CustomerRoleDto.From(reloaded);
    }
}
