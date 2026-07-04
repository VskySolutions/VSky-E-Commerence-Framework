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
        await SeedIntegrationProvidersAsync(cancellationToken);
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

    /// <summary>
    /// Seeds the dynamic Credential Vault catalogue (WO-7): integration categories, providers, and their
    /// credential field definitions. Idempotent at the provider level — re-running adds newly-shipped
    /// providers without disturbing existing ones or their stored values.
    /// </summary>
    private async Task SeedIntegrationProvidersAsync(CancellationToken ct)
    {
        var categoriesByCode = await _db.IntegrationCategories.ToDictionaryAsync(c => c.Code, ct);
        var existingProviderCodes = (await _db.IntegrationProviders
                .IgnoreQueryFilters()
                .Select(p => p.Code)
                .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var addedProviders = 0;
        var catalogue = DefaultIntegrationProviders.Catalogue;

        for (var ci = 0; ci < catalogue.Length; ci++)
        {
            var catSeed = catalogue[ci];
            if (!categoriesByCode.TryGetValue(catSeed.Code, out var category))
            {
                category = new IntegrationCategory
                {
                    Name = catSeed.Name,
                    Code = catSeed.Code,
                    Description = catSeed.Description,
                    DisplayOrder = ci,
                };
                _db.IntegrationCategories.Add(category);
                categoriesByCode[catSeed.Code] = category;
            }

            for (var pi = 0; pi < catSeed.Providers.Length; pi++)
            {
                var provSeed = catSeed.Providers[pi];
                if (existingProviderCodes.Contains(provSeed.Code))
                    continue;

                _db.IntegrationProviders.Add(new IntegrationProvider
                {
                    Category = category,
                    Name = provSeed.Name,
                    Code = provSeed.Code,
                    Description = provSeed.Description,
                    IsEnabled = true,
                    DisplayOrder = pi,
                    Definitions = provSeed.Fields.Select((f, fi) => new CredentialDefinition
                    {
                        FieldName = f.FieldName,
                        FieldCode = f.FieldCode,
                        DataType = f.DataType,
                        IsRequired = f.Required,
                        IsSecret = f.Secret,
                        Placeholder = f.Placeholder,
                        HelpText = f.HelpText,
                        DisplayOrder = fi,
                    }).ToList(),
                });
                addedProviders++;
            }
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} integration providers.", addedProviders);
        }
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
