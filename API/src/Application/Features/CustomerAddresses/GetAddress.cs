using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.CustomerAddresses;

/// <summary>Gets one of the authenticated customer's addresses by id (ownership enforced).</summary>
public record GetAddressQuery(Guid Id) : IRequest<AddressDto>;

public class GetAddressQueryHandler : IRequestHandler<GetAddressQuery, AddressDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public GetAddressQueryHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<AddressDto> Handle(GetAddressQuery request, CancellationToken cancellationToken)
    {
        if (_current.UserId is null)
            throw new UnauthorizedException();

        var customerId = await _db.Customers
            .Where(c => c.UserId == _current.UserId.Value)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenAccessException("The current user does not have a customer profile.");

        // Scoped to the current customer: a foreign or missing id is indistinguishable (both 404).
        var address = await _db.Addresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.CustomerId == customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Address), request.Id);

        return AddressDto.From(address);
    }
}
