using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Application.Features.ApiKeys;

/// <summary>Revokes an API key (deactivates it); subsequent authentication attempts are rejected with 401.</summary>
public record RevokeApiKeyCommand(Guid Id) : IRequest;

public class RevokeApiKeyCommandHandler : IRequestHandler<RevokeApiKeyCommand>
{
    private readonly IApplicationDbContext _db;

    public RevokeApiKeyCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        var key = await _db.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ApiKey), request.Id);

        key.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
