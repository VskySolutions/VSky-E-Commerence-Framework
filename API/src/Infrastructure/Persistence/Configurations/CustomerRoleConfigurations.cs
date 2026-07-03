using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VSky.Domain.Entities;

namespace VSky.Infrastructure.Persistence.Configurations;

public class CustomerRoleConfiguration : IEntityTypeConfiguration<CustomerRole>
{
    public void Configure(EntityTypeBuilder<CustomerRole> b)
    {
        b.ToTable("CustomerRoles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(512);
        b.Property(x => x.DiscountPercent).HasPrecision(5, 2);
        b.HasQueryFilter(x => !x.Deleted);
    }
}

public class CustomerRoleAssignmentConfiguration : IEntityTypeConfiguration<CustomerRoleAssignment>
{
    public void Configure(EntityTypeBuilder<CustomerRoleAssignment> b)
    {
        b.ToTable("CustomerRoleAssignments");
        b.HasKey(x => x.Id);
        b.HasOne(x => x.CustomerRole).WithMany().HasForeignKey(x => x.CustomerRoleId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CustomerId, x.CustomerRoleId }).IsUnique();
        b.HasIndex(x => x.CustomerId);
    }
}

public class CustomerGroupPriceConfiguration : IEntityTypeConfiguration<CustomerGroupPrice>
{
    public void Configure(EntityTypeBuilder<CustomerGroupPrice> b)
    {
        b.ToTable("CustomerGroupPrices");
        b.HasKey(x => x.Id);
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.HasOne(x => x.CustomerRole).WithMany(r => r.GroupPrices).HasForeignKey(x => x.CustomerRoleId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ProductId, x.ProductVariantId });
    }
}

public class ProductRoleRestrictionConfiguration : IEntityTypeConfiguration<ProductRoleRestriction>
{
    public void Configure(EntityTypeBuilder<ProductRoleRestriction> b)
    {
        b.ToTable("ProductRoleRestrictions");
        b.HasKey(x => x.Id);
        b.HasOne(x => x.CustomerRole).WithMany().HasForeignKey(x => x.CustomerRoleId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ProductId, x.CustomerRoleId }).IsUnique();
        b.HasIndex(x => x.ProductId);
    }
}

public class CategoryRoleRestrictionConfiguration : IEntityTypeConfiguration<CategoryRoleRestriction>
{
    public void Configure(EntityTypeBuilder<CategoryRoleRestriction> b)
    {
        b.ToTable("CategoryRoleRestrictions");
        b.HasKey(x => x.Id);
        b.HasOne(x => x.CustomerRole).WithMany().HasForeignKey(x => x.CustomerRoleId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CategoryId, x.CustomerRoleId }).IsUnique();
        b.HasIndex(x => x.CategoryId);
    }
}
