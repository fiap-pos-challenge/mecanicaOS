using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Portal.Entities;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.ServiceOrders.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.Portal;

public sealed class PortalChargeConfiguration : IEntityTypeConfiguration<PortalCharge>
{
    public void Configure(EntityTypeBuilder<PortalCharge> builder)
    {
        builder.ToTable("portalCharges");
        builder.HasKey(charge => charge.Id);
        builder.Property(charge => charge.Id).ValueGeneratedNever();
        builder.Property(charge => charge.Method).HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(charge => charge.Status).HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(charge => charge.TxId).HasMaxLength(25).IsUnicode(false).IsRequired();
        builder.Property(charge => charge.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(charge => charge.ExpiresAt).IsRequired();
        builder.HasOne<Estimate>().WithMany().HasForeignKey(charge => charge.EstimateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ServiceOrder>().WithMany().HasForeignKey(charge => charge.ServiceOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(charge => charge.TxId).IsUnique().HasDatabaseName("ux_portalCharges_txId");
        builder.HasIndex(charge => charge.ServiceOrderId).HasDatabaseName("ix_portalCharges_serviceOrderId");
    }
}
