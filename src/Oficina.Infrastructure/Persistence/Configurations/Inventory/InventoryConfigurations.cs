using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Inventory.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Inventory;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(supplier => supplier.Id);
        builder.Property(supplier => supplier.Id).ValueGeneratedNever();
        builder.Ignore(supplier => supplier.DomainEvents);
        builder.Property(supplier => supplier.Name).HasMaxLength(Supplier.NameMaxLength).IsRequired();
        builder.Property(supplier => supplier.Document).HasMaxLength(14).IsUnicode(false);
        builder.Property(supplier => supplier.Email).HasMaxLength(160);
        builder.Property(supplier => supplier.Phone).HasMaxLength(Supplier.PhoneMaxLength).IsUnicode(false);
        builder.Property(supplier => supplier.LeadTimeDays).IsRequired();
        builder.Property(supplier => supplier.IsActive).IsRequired();
        builder.HasIndex(supplier => supplier.Document).IsUnique().HasFilter("[document] IS NOT NULL AND [deletedAt] IS NULL").HasDatabaseName("ux_suppliers_document");
    }
}

public sealed class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.ToTable("stockCounts");
        builder.HasKey(count => count.Id);
        builder.Property(count => count.Id).ValueGeneratedNever();
        builder.Ignore(count => count.DomainEvents);
        builder.Ignore(count => count.IsOpen);
        builder.Property(count => count.Location).HasMaxLength(Part.LocationMaxLength).IsUnicode(false).IsRequired();
        builder.Property(count => count.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(count => count.RejectionReason).HasMaxLength(500);
        builder.Property(count => count.Lines).HasJsonConversion().IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(count => count.OpenedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(count => count.DecidedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchaseOrders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Ignore(order => order.DomainEvents);
        builder.Ignore(order => order.Total);
        builder.Property(order => order.Number).IsRequired();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(order => order.Lines).HasJsonConversion().IsRequired();
        builder.HasOne<Supplier>().WithMany().HasForeignKey(order => order.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(order => order.Number).IsUnique().HasDatabaseName("ux_purchaseOrders_number");
    }
}
