using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.ServiceOrders;

public sealed class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
{
    public void Configure(EntityTypeBuilder<ServiceItem> builder)
    {
        builder.ToTable("serviceItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property(item => item.Description).HasMaxLength(Service.NameMaxLength).IsRequired();
        builder.Property(item => item.UnitPrice).HasConversion<MoneyConverter>().HasPrecision(12, 2).IsRequired();
        builder.Property(item => item.Quantity).IsRequired();

        builder.HasOne<Service>().WithMany().HasForeignKey(item => item.ServiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
