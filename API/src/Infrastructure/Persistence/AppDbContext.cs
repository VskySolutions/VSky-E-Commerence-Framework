using System.Reflection;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Common;
using VSky.Domain.Entities;

namespace VSky.Infrastructure.Persistence;

/// <summary>
/// The single EF Core persistence context for the deployment. Applies audit metadata and converts
/// hard deletes of soft-deletable entities into soft deletes on save.
/// </summary>
public class AppDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUser;
    private readonly IDateTimeProvider? _clock;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService? currentUser = null,
        IDateTimeProvider? clock = null)
        : base(options)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TenantCredential> TenantCredentials => Set<TenantCredential>();
    public DbSet<SmtpAccount> SmtpAccounts => Set<SmtpAccount>();
    public DbSet<TenantBranding> TenantBrandings => Set<TenantBranding>();
    public DbSet<SupportedCurrency> SupportedCurrencies => Set<SupportedCurrency>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<DeliveryZone> DeliveryZones => Set<DeliveryZone>();
    public DbSet<AdminAlert> AdminAlerts => Set<AdminAlert>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();
    public DbSet<SettingsChangeHistory> SettingsChangeHistory => Set<SettingsChangeHistory>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailQueue> EmailQueue => Set<EmailQueue>();
    public DbSet<MarketingSuppression> MarketingSuppressions => Set<MarketingSuppression>();
    public DbSet<ApplicationLog> ApplicationLogs => Set<ApplicationLog>();
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();
    public DbSet<BackgroundTaskLog> BackgroundTaskLogs => Set<BackgroundTaskLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    private void ApplyAuditInformation()
    {
        var now = _clock?.UtcNow ?? DateTime.UtcNow;
        var userId = _currentUser?.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOnUtc = now;
                    entry.Entity.CreatedById = userId;
                    entry.Entity.UpdatedOnUtc = now;
                    entry.Entity.UpdatedById = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedOnUtc = now;
                    entry.Entity.UpdatedById = userId;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.Deleted = true;
                entry.Entity.DeletedOnUtc = now;
            }
        }
    }
}
