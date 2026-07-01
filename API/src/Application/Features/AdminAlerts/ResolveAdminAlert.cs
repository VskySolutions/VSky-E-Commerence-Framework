using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;

namespace VSky.Application.Features.AdminAlerts;

/// <summary>Marks an admin alert as resolved.</summary>
public record ResolveAdminAlertCommand(Guid Id) : IRequest;

public class ResolveAdminAlertCommandHandler : IRequestHandler<ResolveAdminAlertCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public ResolveAdminAlertCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(ResolveAdminAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = await _db.AdminAlerts.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("AdminAlert", request.Id);

        alert.IsResolved = true;
        alert.ResolvedOnUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
