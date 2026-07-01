using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VSky.Application.Common.Interfaces;
using VSky.Infrastructure.Alerts;
using VSky.Infrastructure.Credentials;
using VSky.Infrastructure.Currencies;
using VSky.Infrastructure.Persistence;
using VSky.Infrastructure.BackgroundTasks;
using VSky.Infrastructure.BackgroundTasks.Workers;
using VSky.Infrastructure.Email;
using VSky.Infrastructure.Security;
using VSky.Infrastructure.Services;
using VSky.Infrastructure.Settings;
using VSky.Infrastructure.Storage;

namespace VSky.Infrastructure;

/// <summary>Registers Infrastructure-layer services: EF Core context, clock, database initializer.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            // RefreshToken/UserRole → User (soft-deleted) is intentional; a filtered-out user simply
            // yields a null navigation, which the refresh handler treats as an invalid token.
            options.ConfigureWarnings(w =>
                w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<DatabaseInitializer>();

        // Authentication / security (WO-1).
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<RsaKeyProvider>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // API key authentication for machine-to-machine callers (WO-95).
        services.AddScoped<IApiKeyService, ApiKeyService>();

        // Credential vault (WO-7): deployment-specific Data Protection key ring persisted outside the DB.
        var dpKeysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(dpKeysPath))
            dpKeysPath = "keys/dataprotection";
        services.AddDataProtection()
            .SetApplicationName("VSky.ECommerce")
            .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));

        services.AddHttpClient();
        services.AddScoped<ICredentialVault, CredentialVault>();
        services.AddScoped<ICredentialConnectivityChecker, CredentialConnectivityChecker>();

        // SMTP account management (WO-75).
        services.AddScoped<ISmtpTester, MailKitSmtpTester>();

        // Settings (WO-3): cached, audited platform settings.
        services.AddMemoryCache();
        services.AddScoped<ISettingsService, SettingsService>();

        // Audit-finding resolutions: currency auto-refresh (WO-90) + admin alerts (WO-75 AC-TEN-003.5).
        services.AddScoped<ICurrencyRateRefresher, CurrencyRateRefresher>();
        services.AddScoped<IAdminAlertService, AdminAlertService>();

        // File storage (WO-88): provider-agnostic service with Local + Azure Blob adapters.
        services.AddScoped<IFileStorageAdapter, LocalFilesystemAdapter>();
        services.AddScoped<IFileStorageAdapter, AzureBlobAdapter>();
        services.AddScoped<IFileStorage, FileStorageService>();

        // Background task scheduler (WO-4).
        services.AddSingleton<TaskScheduleRegistry>();
        services.AddSingleton<IBackgroundTaskStatusProvider>(sp => sp.GetRequiredService<TaskScheduleRegistry>());
        services.AddSingleton<IScheduledTask, AbandonedCartWorker>();
        services.AddSingleton<IScheduledTask, LowStockAlertWorker>();
        services.AddSingleton<IScheduledTask, TrackingSyncWorker>();
        services.AddSingleton<IScheduledTask, DatabaseCleanupWorker>();
        services.AddHostedService<TaskSchedulerService>();

        return services;
    }
}
