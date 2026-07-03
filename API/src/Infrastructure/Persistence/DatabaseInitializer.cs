using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VSky.Domain.Entities;
using VSky.Domain.Enums;
using VSky.Infrastructure.Persistence.Seed;

namespace VSky.Infrastructure.Persistence;

/// <summary>
/// Applies pending EF Core migrations and seeds baseline data. Migration must complete before the
/// API Server accepts traffic (API Server / Database blueprints).
/// </summary>
public class DatabaseInitializer
{
    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext db, ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational())
            return;

        _logger.LogInformation("Applying database migrations...");
        await _db.Database.MigrateAsync(cancellationToken);
        _logger.LogInformation("Database migrations applied.");
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(cancellationToken);
        await SeedPlatformSettingsAsync(cancellationToken);
        await SeedEmailTemplatesAsync(cancellationToken);
        await SeedBaseCurrencyAsync(cancellationToken);
        await SeedTaxCategoriesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var systemRoles = new (string Name, string Description)[]
        {
            (nameof(RoleType.SuperAdmin), "Platform-wide access."),
            (nameof(RoleType.TenantAdmin), "Full access within the deployment."),
        };

        var existing = await _db.Roles.Select(r => r.Name).ToListAsync(ct);
        var toAdd = systemRoles
            .Where(r => !existing.Contains(r.Name))
            .Select(r => new Role
            {
                Name = r.Name,
                NormalizedName = r.Name.ToUpperInvariant(),
                Description = r.Description,
                IsSystemRole = true,
            })
            .ToList();

        if (toAdd.Count > 0)
        {
            await _db.Roles.AddRangeAsync(toAdd, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} system roles.", toAdd.Count);
        }
    }

    private async Task SeedPlatformSettingsAsync(CancellationToken ct)
    {
        var existingKeys = await _db.PlatformSettings.Select(s => s.Key).ToListAsync(ct);
        var toAdd = DefaultPlatformSettings.Build()
            .Where(s => !existingKeys.Contains(s.Key))
            .ToList();

        if (toAdd.Count > 0)
        {
            await _db.PlatformSettings.AddRangeAsync(toAdd, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} default platform settings.", toAdd.Count);
        }
    }

    private async Task SeedEmailTemplatesAsync(CancellationToken ct)
    {
        var existingKeys = await _db.EmailTemplates.Select(t => t.TemplateKey).ToListAsync(ct);
        var toAdd = DefaultEmailTemplates.Build()
            .Where(t => !existingKeys.Contains(t.TemplateKey))
            .ToList();

        if (toAdd.Count > 0)
        {
            await _db.EmailTemplates.AddRangeAsync(toAdd, ct);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} default email templates.", toAdd.Count);
        }
    }

    private async Task SeedBaseCurrencyAsync(CancellationToken ct)
    {
        if (await _db.SupportedCurrencies.AnyAsync(ct))
            return;

        _db.SupportedCurrencies.Add(new SupportedCurrency
        {
            CurrencyCode = "USD",
            Symbol = "$",
            ExchangeRate = 1m,
            IsEnabled = true,
            IsBaseCurrency = true,
            IsRateLocked = true,
        });
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded default base currency (USD).");
    }

    private async Task SeedTaxCategoriesAsync(CancellationToken ct)
    {
        // Every product must reference a tax category (AC-CAT-001.6); guarantee at least one exists so
        // the catalog is usable out of the box. Authoritative rates are owned by the Tax feature.
        if (await _db.TaxCategories.AnyAsync(ct))
            return;

        _db.TaxCategories.Add(new TaxCategory
        {
            Name = "Standard",
            Description = "Default tax category.",
            DisplayOrder = 0,
            IsActive = true,
        });
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded default tax category (Standard).");
    }
}

/// <summary>Startup helper to run migration + seed within a fresh DI scope.</summary>
public static class DatabaseInitializerExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.MigrateAsync(cancellationToken);
        await initializer.SeedAsync(cancellationToken);
    }
}
