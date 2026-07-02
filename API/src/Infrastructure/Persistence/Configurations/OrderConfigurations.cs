using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VSky.Domain.Entities;

namespace VSky.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.HasKey(x => x.Id);
        b.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.ContactName).HasMaxLength(200);
        b.Property(x => x.ContactEmail).HasMaxLength(256);
        b.Property(x => x.CountryCode).HasMaxLength(2);
        b.Property(x => x.Region).HasMaxLength(120);
        b.Property(x => x.PostalCode).HasMaxLength(20);
        b.Property(x => x.AddressLine1).HasMaxLength(255);
        b.Property(x => x.AddressLine2).HasMaxLength(255);
        b.Property(x => x.City).HasMaxLength(120);
        b.Property(x => x.StateProvince).HasMaxLength(120);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);

        b.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasOne(x => x.AssignedStore)
            .WithMany()
            .HasForeignKey(x => x.AssignedStoreId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasIndex(x => x.OrderNumber).IsUnique();
        b.HasIndex(x => new { x.AssignedStoreId, x.Status });
        b.HasIndex(x => x.Status);
        b.HasQueryFilter(x => !x.Deleted);
    }
}

public class OrderLineItemConfiguration : IEntityTypeConfiguration<OrderLineItem>
{
    public void Configure(EntityTypeBuilder<OrderLineItem> b)
    {
        b.ToTable("OrderLineItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProductName).HasMaxLength(400).IsRequired();
        b.Property(x => x.Sku).HasMaxLength(100);
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.LineTotal).HasPrecision(18, 2);

        b.HasOne(x => x.Order)
            .WithMany(o => o.Lines)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.OrderId);
        b.HasIndex(x => x.ProductId);
    }
}

public class StoreManagerAssignmentConfiguration : IEntityTypeConfiguration<StoreManagerAssignment>
{
    public void Configure(EntityTypeBuilder<StoreManagerAssignment> b)
    {
        b.ToTable("StoreManagerAssignments");
        b.HasKey(x => x.Id);

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.NoAction);

        // One store per manager.
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasIndex(x => x.StoreId);
    }
}
