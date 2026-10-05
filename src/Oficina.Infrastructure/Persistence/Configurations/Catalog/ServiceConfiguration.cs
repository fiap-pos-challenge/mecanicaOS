using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Catalog.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("services");
        builder.HasKey(service => service.Id);
        builder.Property(service => service.Id).ValueGeneratedNever();
        builder.Ignore(service => service.DomainEvents);

        builder.Property(service => service.Name).HasMaxLength(Service.NameMaxLength).IsRequired();
        builder.Property(service => service.Description);
        builder.Property(service => service.BasePrice).HasConversion<MoneyConverter>().HasPrecision(12, 2).IsRequired();
        builder.Property(service => service.EstimatedMinutes).IsRequired();
        builder.Property(service => service.IsActive).IsRequired();
    }
}
