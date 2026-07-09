using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.CustomerAddresses;

/// <summary>Soft-deletes one of the authenticated customer's addresses (ownership enforced).</summary>
public record DeleteAddressCommand(Guid Id) : IRequest;

public class DeleteAddressCommandHandler : IRequestHandler<DeleteAddressCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public DeleteAddressCommandHandler(IApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        if (_current.UserId is null)
            throw new UnauthorizedException();

        var customerId = await _db.Customers
            .Where(c => c.UserId == _current.UserId.Value)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenAccessException("The current user does not have a customer profile.");

        var entry = await _db.CustomerAddresses
            .Include(m => m.Address)
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.CustomerId == customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerAddress), request.Id);

        // Soft-delete the book entry and its (customer-owned) address row.
        _db.CustomerAddresses.Remove(entry);
        if (entry.Address is not null)
            _db.Addresses.Remove(entry.Address);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
