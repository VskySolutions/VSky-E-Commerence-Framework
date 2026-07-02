using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CustomerAddresses;

/// <summary>Updates one of the authenticated customer's addresses (ownership enforced; AC-CUS-002.2).</summary>
public record UpdateAddressCommand(
    Guid Id,
    AddressType AddressType,
    bool IsDefault,
    string FirstName,
    string LastName,
    string? Company,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? StateProvince,
    string PostalCode,
    string CountryCode,
    string? PhoneNumber) : IRequest<AddressDto>;

public class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AddressType).IsInEnum();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Company).MaximumLength(200);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(255);
        RuleFor(x => x.AddressLine2).MaximumLength(255);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.StateProvince).MaximumLength(120);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);
        RuleFor(x => x.PhoneNumber).MaximumLength(50);
    }
}

public class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand, AddressDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public UpdateAddressCommandHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<AddressDto> Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
    {
        if (_current.UserId is null)
            throw new UnauthorizedException();

        var customerId = await _db.Customers
            .Where(c => c.UserId == _current.UserId.Value)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenAccessException("The current user does not have a customer profile.");

        var entity = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.CustomerId == customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Address), request.Id);

        // A type must always have a default; if this is the only address of its (possibly new) type,
        // keep it the default automatically (AC-CUS-002.3).
        var hasOtherOfType = await _db.Addresses.AnyAsync(
            a => a.CustomerId == customerId && a.AddressType == request.AddressType && a.Id != entity.Id,
            cancellationToken);
        var makeDefault = request.IsDefault || !hasOtherOfType;

        if (makeDefault)
            await ClearExistingDefaultsAsync(customerId, request.AddressType, entity.Id, cancellationToken);

        entity.AddressType = request.AddressType;
        entity.IsDefault = makeDefault;
        entity.FirstName = request.FirstName.Trim();
        entity.LastName = request.LastName.Trim();
        entity.Company = request.Company;
        entity.AddressLine1 = request.AddressLine1.Trim();
        entity.AddressLine2 = request.AddressLine2;
        entity.City = request.City.Trim();
        entity.StateProvince = request.StateProvince;
        entity.PostalCode = request.PostalCode.Trim();
        entity.CountryCode = request.CountryCode.Trim().ToUpperInvariant();
        entity.PhoneNumber = request.PhoneNumber;

        await _db.SaveChangesAsync(cancellationToken);

        return AddressDto.From(entity);
    }

    // Clears (and persists) any other default of the same type before this one becomes the default, so
    // the filtered unique index (one default per customer+type) is never transiently violated.
    private async Task ClearExistingDefaultsAsync(
        Guid customerId, AddressType type, Guid excludeId, CancellationToken cancellationToken)
    {
        var currentDefaults = await _db.Addresses
            .Where(a => a.CustomerId == customerId && a.AddressType == type && a.Id != excludeId && a.IsDefault)
            .ToListAsync(cancellationToken);

        if (currentDefaults.Count == 0)
            return;

        foreach (var a in currentDefaults)
            a.IsDefault = false;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
