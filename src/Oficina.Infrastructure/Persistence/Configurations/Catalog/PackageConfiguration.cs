using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Catalog.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Catalog;

public sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("packages");
        builder.HasKey(package => package.Id);
        builder.Property(package => package.Id).ValueGeneratedNever();
        builder.Ignore(package => package.DomainEvents);

        builder.Property(package => package.Name).HasMaxLength(Package.NameMaxLength).IsRequired();
        builder.Property(package => package.Description).HasMaxLength(Package.DescriptionMaxLength);
        builder.Property(package => package.Price).HasConversion<MoneyConverter>().HasPrecision(12, 2).IsRequired();
        builder.Property(package => package.MinMileage);
        builder.Property(package => package.MaxMileage);
        builder.Property(package => package.IsActive).IsRequired();

        builder.OwnsMany(package => package.Items, item =>
        {
            item.ToTable("packageItems");
            item.WithOwner().HasForeignKey("packageId");
            item.HasKey(line => line.Id);
            item.Property(line => line.Id).ValueGeneratedNever();
            item.Property(line => line.Kind).HasConversion<string>().HasMaxLength(10).IsUnicode(false).IsRequired();
            item.Property(line => line.ItemId).IsRequired();
            item.Property(line => line.Quantity).HasConversion<QuantityConverter>().HasPrecision(12, 3).IsRequired();
            item.HasIndex(line => new { line.Kind, line.ItemId }).HasDatabaseName("ix_packageItems_item");
        });
        builder.Navigation(package => package.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
