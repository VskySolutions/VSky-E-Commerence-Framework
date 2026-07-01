using Microsoft.EntityFrameworkCore;
using VSky.Domain.Entities;

namespace VSky.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the persistence context exposed to Application-layer handlers, keeping them
/// free of a direct Infrastructure/EF Core provider dependency.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<ApiKey> ApiKeys { get; }
    DbSet<Customer> Customers { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<TenantCredential> TenantCredentials { get; }
    DbSet<SmtpAccount> SmtpAccounts { get; }
    DbSet<TenantBranding> TenantBrandings { get; }
    DbSet<SupportedCurrency> SupportedCurrencies { get; }
    DbSet<Store> Stores { get; }
    DbSet<DeliveryZone> DeliveryZones { get; }
    DbSet<AdminAlert> AdminAlerts { get; }
    DbSet<PlatformSetting> PlatformSettings { get; }
    DbSet<SettingsChangeHistory> SettingsChangeHistory { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<EmailQueue> EmailQueue { get; }
    DbSet<MarketingSuppression> MarketingSuppressions { get; }
    DbSet<ApplicationLog> ApplicationLogs { get; }
    DbSet<AuditTrail> AuditTrails { get; }
    DbSet<BackgroundTaskLog> BackgroundTaskLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
