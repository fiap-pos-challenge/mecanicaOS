using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.ServiceOrders;

public sealed class PartItemConfiguration : IEntityTypeConfiguration<PartItem>
{
    public void Configure(EntityTypeBuilder<PartItem> builder)
    {
        builder.ToTable("partItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property(item => item.Description).HasMaxLength(Part.NameMaxLength).IsRequired();
        builder.Property(item => item.UnitPrice).HasConversion<MoneyConverter>().HasPrecision(12, 2).IsRequired();
        builder.Property(item => item.Quantity).HasConversion<QuantityConverter>().HasPrecision(12, 3).IsRequired();

        builder.HasOne<Part>().WithMany().HasForeignKey(item => item.PartId).OnDelete(DeleteBehavior.Restrict);
    }
}
