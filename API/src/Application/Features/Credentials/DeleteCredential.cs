using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;

namespace VSky.Application.Features.Credentials;

/// <summary>Removes a stored credential (idempotent).</summary>
public record DeleteCredentialCommand(string ServiceType) : IRequest;

public class DeleteCredentialCommandHandler : IRequestHandler<DeleteCredentialCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCredentialCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCredentialCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.TenantCredentials
            .FirstOrDefaultAsync(c => c.ServiceType == request.ServiceType, cancellationToken);

        if (entity is null)
            return;

        _db.TenantCredentials.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
