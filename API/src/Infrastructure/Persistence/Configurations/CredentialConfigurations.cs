using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VSky.Domain.Entities;

namespace VSky.Infrastructure.Persistence.Configurations;

public class TenantCredentialConfiguration : IEntityTypeConfiguration<TenantCredential>
{
    public void Configure(EntityTypeBuilder<TenantCredential> b)
    {
        b.ToTable("TenantCredentials");
        b.HasKey(x => x.Id);
        b.Property(x => x.ServiceType).HasMaxLength(100).IsRequired();
        b.Property(x => x.EncryptedValue).IsRequired();
        b.Property(x => x.LastFourChars).HasMaxLength(8);
        b.Property(x => x.Description).HasMaxLength(500);

        // One stored credential per service type per deployment.
        b.HasIndex(x => x.ServiceType).IsUnique();
    }
}

public class SmtpAccountConfiguration : IEntityTypeConfiguration<SmtpAccount>
{
    public void Configure(EntityTypeBuilder<SmtpAccount> b)
    {
        b.ToTable("SmtpAccounts");
        b.HasKey(x => x.Id);
        b.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Host).HasMaxLength(255).IsRequired();
        b.Property(x => x.Username).HasMaxLength(255);
        b.Property(x => x.FromName).HasMaxLength(200).IsRequired();
        b.Property(x => x.FromEmail).HasMaxLength(255).IsRequired();
    }
}
