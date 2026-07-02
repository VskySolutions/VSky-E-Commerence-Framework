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

        var entity = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.CustomerId == customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Address), request.Id);

        // Soft-delete is applied by the DbContext SaveChanges interceptor (sets Deleted + DeletedOnUtc).
        _db.Addresses.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
