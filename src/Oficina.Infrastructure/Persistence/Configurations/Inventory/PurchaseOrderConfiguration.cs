using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Inventory.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Inventory;

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
