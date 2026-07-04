using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VSky.Domain.Entities;

namespace VSky.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF configuration for the dynamic Credential Vault (WO-7). Cascade paths are deliberately shaped to
/// avoid SQL Server multiple-cascade-path errors: definitions and credential values are owned children of
/// their provider (cascade), while a credential's second FK to its definition uses NoAction.
/// </summary>
public class IntegrationCategoryConfiguration : IEntityTypeConfiguration<IntegrationCategory>
{
    public void Configure(EntityTypeBuilder<IntegrationCategory> b)
    {
        b.ToTable("IntegrationCategories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Code).IsRequired().HasMaxLength(50);
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class IntegrationProviderConfiguration : IEntityTypeConfiguration<IntegrationProvider>
{
    public void Configure(EntityTypeBuilder<IntegrationProvider> b)
    {
        b.ToTable("IntegrationProviders");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Code).IsRequired().HasMaxLength(50);
        b.Property(x => x.Description).HasMaxLength(500);

        b.HasOne(x => x.Category)
            .WithMany(c => c.Providers)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // lookup FK — no cascade from category

        // Provider code is the runtime service-type key; unique among live providers (reusable after retire).
        b.HasIndex(x => x.Code).IsUnique().HasFilter("[Deleted] = 0");
        b.HasQueryFilter(x => !x.Deleted);
    }
}

public class CredentialDefinitionConfiguration : IEntityTypeConfiguration<CredentialDefinition>
{
    public void Configure(EntityTypeBuilder<CredentialDefinition> b)
    {
        b.ToTable("CredentialDefinitions");
        b.HasKey(x => x.Id);
        b.Property(x => x.FieldName).IsRequired().HasMaxLength(100);
        b.Property(x => x.FieldCode).IsRequired().HasMaxLength(50);
        b.Property(x => x.Placeholder).HasMaxLength(200);
        b.Property(x => x.HelpText).HasMaxLength(500);

        b.HasOne(x => x.Provider)
            .WithMany(p => p.Definitions)
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Cascade); // owned child of its provider

        b.HasIndex(x => new { x.ProviderId, x.FieldCode }).IsUnique();
    }
}

public class IntegrationCredentialConfiguration : IEntityTypeConfiguration<IntegrationCredential>
{
    public void Configure(EntityTypeBuilder<IntegrationCredential> b)
    {
        b.ToTable("IntegrationCredentials");
        b.HasKey(x => x.Id);
        b.Property(x => x.Value).IsRequired();
        b.Property(x => x.LastFourChars).HasMaxLength(4);

        b.HasOne(x => x.Provider)
            .WithMany(p => p.Credentials)
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Cascade); // single cascade path — owned by provider

        b.HasOne(x => x.Definition)
            .WithMany(d => d.Values)
            .HasForeignKey(x => x.DefinitionId)
            .OnDelete(DeleteBehavior.NoAction); // second FK — avoid multiple cascade paths

        b.HasIndex(x => new { x.ProviderId, x.DefinitionId }).IsUnique();
    }
}
