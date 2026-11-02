using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Inventory;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stockMovements");
        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Id).ValueGeneratedNever();

        builder.Property(movement => movement.Type).HasConversion<string>().HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(movement => movement.Quantity).HasConversion<QuantityConverter>().HasPrecision(12, 3).IsRequired();
        builder.Property(movement => movement.BalanceAfter).HasPrecision(12, 3).IsRequired();
        builder.Property(movement => movement.Reason).IsRequired();

        builder.HasOne<ServiceOrder>().WithMany().HasForeignKey(movement => movement.ServiceOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(movement => movement.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
