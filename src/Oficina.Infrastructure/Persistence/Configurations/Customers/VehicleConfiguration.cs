using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Infrastructure.Persistence.Configurations.Customers;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");
        builder.HasKey(vehicle => vehicle.Id);
        builder.Property(vehicle => vehicle.Id).ValueGeneratedNever();
        builder.Ignore(vehicle => vehicle.DomainEvents);

        builder.Property(vehicle => vehicle.LicensePlate)
            .HasConversion(plate => plate.Value, value => LicensePlate.Create(value).Value)
            .HasMaxLength(7)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(vehicle => vehicle.Brand).HasMaxLength(Vehicle.BrandMaxLength).IsRequired();
        builder.Property(vehicle => vehicle.Model).HasMaxLength(Vehicle.ModelMaxLength).IsRequired();
        builder.Property(vehicle => vehicle.Year)
            .HasConversion(year => (short)year.Value, value => VehicleYear.Create(value).Value)
            .HasColumnType("smallint")
            .IsRequired();

        builder.HasOne<Customer>().WithMany().HasForeignKey(vehicle => vehicle.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(vehicle => vehicle.LicensePlate).IsUnique().HasFilter("[deletedAt] IS NULL").HasDatabaseName("ux_vehicles_licensePlate");
    }
}
