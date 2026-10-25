using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Inventory.Entities;

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
