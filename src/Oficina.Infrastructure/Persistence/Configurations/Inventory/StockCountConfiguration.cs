using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Inventory.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Inventory;

public sealed class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.ToTable("stockCounts");
        builder.HasKey(count => count.Id);
        builder.Property(count => count.Id).ValueGeneratedNever();
        builder.Ignore(count => count.DomainEvents);
        builder.Ignore(count => count.IsOpen);
        builder.Property(count => count.Location).HasMaxLength(Part.LocationMaxLength).IsUnicode(false).IsRequired();
        builder.Property(count => count.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(count => count.RejectionReason).HasMaxLength(500);
        builder.Property(count => count.Lines).HasJsonConversion().IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(count => count.OpenedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(count => count.DecidedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
