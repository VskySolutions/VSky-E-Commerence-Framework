using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;

namespace VSky.Infrastructure.Alerts;

/// <summary>Persists admin alerts raised by features so they can be surfaced in the admin console.</summary>
public class AdminAlertService : IAdminAlertService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminAlertService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task RaiseAsync(string alertType, string title, string? message = null, string severity = "Warning", string? source = null, CancellationToken cancellationToken = default)
    {
        _db.AdminAlerts.Add(new AdminAlert
        {
            AlertType = alertType,
            Title = title,
            Message = message,
            Severity = severity,
            Source = source,
            CreatedOnUtc = _clock.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
