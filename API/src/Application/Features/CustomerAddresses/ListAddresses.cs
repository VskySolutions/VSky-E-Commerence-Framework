using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;

namespace VSky.Application.Features.CustomerAddresses;

/// <summary>Lists all of the authenticated customer's saved addresses (AC-CUS-002.2).</summary>
public record ListAddressesQuery : IRequest<List<AddressDto>>;

public class ListAddressesQueryHandler : IRequestHandler<ListAddressesQuery, List<AddressDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public ListAddressesQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<List<AddressDto>> Handle(ListAddressesQuery request, CancellationToken cancellationToken)
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
            .OrderBy(a => a.AddressType)
            .ThenByDescending(a => a.IsDefault)
            .ThenBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToListAsync(cancellationToken);

        return addresses.Select(AddressDto.From).ToList();
    }
}
