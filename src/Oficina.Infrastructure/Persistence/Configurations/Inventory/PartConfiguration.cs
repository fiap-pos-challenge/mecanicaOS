using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.ValueObjects;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Inventory;

public sealed class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.ToTable("parts");
        builder.HasKey(part => part.Id);
        builder.Property(part => part.Id).ValueGeneratedNever();
        builder.Ignore(part => part.DomainEvents);

        builder.Property(part => part.Code)
            .HasConversion(code => code.Value, value => PartCode.Create(value).Value)
            .HasMaxLength(PartCode.MaxLength)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(part => part.Name).HasMaxLength(Part.NameMaxLength).IsRequired();
        builder.Property(part => part.Type).HasConversion<string>().HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(part => part.UnitOfMeasure).HasMaxLength(Part.UnitOfMeasureMaxLength).IsUnicode(false).IsRequired();
        builder.Property(part => part.UnitPrice).HasConversion<MoneyConverter>().HasPrecision(12, 2).IsRequired();
        builder.Property(part => part.Cost).HasConversion<MoneyConverter>().HasPrecision(12, 2).IsRequired();
        builder.Property(part => part.Supplier).HasMaxLength(Part.SupplierMaxLength);
        builder.Property(part => part.Barcode).HasMaxLength(Part.BarcodeLength).IsUnicode(false);
        builder.Property(part => part.StockQuantity).HasPrecision(12, 3).IsRequired();
        builder.Property(part => part.MinimumStock).HasPrecision(12, 3).IsRequired();
        builder.Property(part => part.IsActive).IsRequired();
        builder.Property(part => part.Location).HasMaxLength(Part.LocationMaxLength).IsUnicode(false).IsRequired().HasDefaultValue(Part.DefaultLocation);
        builder.Property<byte[]>(ConcurrencyToken.RowVersion).IsRowVersion().IsRequired();

        builder.HasMany(part => part.StockMovements)
            .WithOne()
            .HasForeignKey(movement => movement.PartId)
            .OnDelete(DeleteBehavior.ClientCascade);
        builder.Navigation(part => part.StockMovements).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(part => part.Code).IsUnique().HasFilter("[deletedAt] IS NULL").HasDatabaseName("ux_parts_code");
    }
}
