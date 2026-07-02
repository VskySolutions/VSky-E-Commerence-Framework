using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CustomerAddresses;

/// <summary>Adds a new address to the authenticated customer's address book (AC-CUS-002.2).</summary>
public record CreateAddressCommand(
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

public class CreateAddressCommandValidator : AbstractValidator<CreateAddressCommand>
{
    public CreateAddressCommandValidator()
    {
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

public class CreateAddressCommandHandler : IRequestHandler<CreateAddressCommand, AddressDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public CreateAddressCommandHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<AddressDto> Handle(CreateAddressCommand request, CancellationToken cancellationToken)
    {
        if (_current.UserId is null)
            throw new UnauthorizedException();

        var customerId = await _db.Customers
            .Where(c => c.UserId == _current.UserId.Value)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenAccessException("The current user does not have a customer profile.");

        // The first address of a type is defaulted automatically (AC-CUS-002.3).
        var hasExistingOfType = await _db.Addresses
            .AnyAsync(a => a.CustomerId == customerId && a.AddressType == request.AddressType, cancellationToken);
        var makeDefault = request.IsDefault || !hasExistingOfType;

        if (makeDefault)
            await ClearExistingDefaultsAsync(customerId, request.AddressType, cancellationToken);

        var entity = new Address
        {
            CustomerId = customerId,
            AddressType = request.AddressType,
            IsDefault = makeDefault,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Company = request.Company,
            AddressLine1 = request.AddressLine1.Trim(),
            AddressLine2 = request.AddressLine2,
            City = request.City.Trim(),
            StateProvince = request.StateProvince,
            PostalCode = request.PostalCode.Trim(),
            CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
            PhoneNumber = request.PhoneNumber,
        };

        _db.Addresses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return AddressDto.From(entity);
    }

    // Clears (and persists) any existing default of the same type before a new default is set, so the
    // filtered unique index (one default per customer+type) is never transiently violated.
    private async Task ClearExistingDefaultsAsync(Guid customerId, AddressType type, CancellationToken cancellationToken)
    {
        var currentDefaults = await _db.Addresses
            .Where(a => a.CustomerId == customerId && a.AddressType == type && a.IsDefault)
            .ToListAsync(cancellationToken);

        if (currentDefaults.Count == 0)
            return;

        foreach (var a in currentDefaults)
            a.IsDefault = false;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
