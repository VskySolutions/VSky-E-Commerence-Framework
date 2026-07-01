using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VSky.Application.Common.Interfaces;

namespace VSky.Infrastructure.BackgroundTasks.Workers;

/// <summary>
/// Refreshes currency exchange rates from the configured external source on schedule (WO-90), skipping
/// locked and base currencies. Carrier tracking sync — its other blueprint duty — lands with the
/// shipping feature (later phase).
/// </summary>
public class TrackingSyncWorker : IScheduledTask
{
    public string Name => "TrackingSyncWorker";
    public TimeSpan DefaultInterval => TimeSpan.FromMinutes(180);
    public string? IntervalSettingKey => "tasks.tracking-sync.interval-minutes";
    public string? CronSettingKey => null;

    public async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(Name);

        // Exchange-rate refresh (WO-90). No-op unless auto-refresh is enabled and a source is configured.
        var refresher = services.GetRequiredService<ICurrencyRateRefresher>();
        var updated = await refresher.RefreshAsync(cancellationToken);
        if (updated > 0)
            logger.LogInformation("{Task} refreshed {Count} currency exchange rate(s).", Name, updated);

        // Carrier tracking sync is pending the shipping feature (later phase).
    }
}
