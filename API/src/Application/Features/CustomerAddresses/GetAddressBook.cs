using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CustomerAddresses;

/// <summary>
/// Returns the authenticated customer's addresses grouped by type (shipping/billing) for checkout
/// address selection (AC-CUS-002.2). Default addresses are listed first within each group.
/// </summary>
public record GetAddressBookQuery : IRequest<AddressBookDto>;

public class GetAddressBookQueryHandler : IRequestHandler<GetAddressBookQuery, AddressBookDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public GetAddressBookQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<AddressBookDto> Handle(GetAddressBookQuery request, CancellationToken cancellationToken)
    {
        if (_current.UserId is null)
            throw new UnauthorizedException();

        var customerId = await _db.Customers
            .Where(c => c.UserId == _current.UserId.Value)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenAccessException("The current user does not have a customer profile.");

        var addresses = await _db.Addresses
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToListAsync(cancellationToken);

        return new AddressBookDto
        {
            Shipping = addresses.Where(a => a.AddressType == AddressType.Shipping).Select(AddressDto.From).ToList(),
            Billing = addresses.Where(a => a.AddressType == AddressType.Billing).Select(AddressDto.From).ToList(),
        };
    }
}
