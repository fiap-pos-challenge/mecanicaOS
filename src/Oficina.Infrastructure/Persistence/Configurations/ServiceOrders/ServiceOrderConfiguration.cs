using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.ValueObjects;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.ServiceOrders;

public sealed class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable("serviceOrders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Ignore(order => order.DomainEvents);
        builder.Ignore(order => order.CurrentQuote);

        builder.Property(order => order.Number)
            .HasConversion(number => number.Value, value => ServiceOrderNumber.Create(value).Value)
            .IsRequired();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(order => order.ProblemDescription).IsRequired();
        builder.Property(order => order.TotalAmount).HasConversion<MoneyConverter>().HasPrecision(12, 2).IsRequired();
        builder.Property<byte[]>(ConcurrencyToken.RowVersion).IsRowVersion().IsRequired();
        builder.Property(order => order.Version).IsRequired();
        builder.Property(order => order.Diagnosis).IsRequired();
        builder.Property(order => order.SentToQuote).IsRequired();
        builder.Property(order => order.Reopened).IsRequired();
        builder.Property(order => order.PhotoCount).IsRequired();
        builder.Ignore(order => order.TimerRunning);
        builder.Property(order => order.Checklist).HasJsonConversion().IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(order => order.MechanicId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(order => order.TimeEntries).WithOne().HasForeignKey(entry => entry.ServiceOrderId).OnDelete(DeleteBehavior.ClientCascade);
        builder.Navigation(order => order.TimeEntries).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Customer>().WithMany().HasForeignKey(order => order.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(order => order.VehicleId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(order => order.ServiceItems).WithOne().HasForeignKey(item => item.ServiceOrderId).OnDelete(DeleteBehavior.ClientCascade);
        builder.HasMany(order => order.PartItems).WithOne().HasForeignKey(item => item.ServiceOrderId).OnDelete(DeleteBehavior.ClientCascade);
        builder.HasMany(order => order.Quotes).WithOne().HasForeignKey(quote => quote.ServiceOrderId).OnDelete(DeleteBehavior.ClientCascade);
        builder.HasMany(order => order.StatusHistory).WithOne().HasForeignKey(history => history.ServiceOrderId).OnDelete(DeleteBehavior.ClientCascade);
        builder.Navigation(order => order.ServiceItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(order => order.PartItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(order => order.Quotes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(order => order.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(order => order.Number).IsUnique().HasDatabaseName("ux_serviceOrders_number");
        builder.HasIndex(order => order.VehicleId, "VehicleForeignKey").HasDatabaseName("ix_serviceOrders_vehicleId");
        builder.HasIndex(order => order.VehicleId, "OpenOrderPerVehicle")
            .IsUnique()
            .HasFilter("[status] <> 'Delivered' AND [deletedAt] IS NULL")
            .HasDatabaseName("ux_serviceOrders_openPerVehicle");
    }
}
