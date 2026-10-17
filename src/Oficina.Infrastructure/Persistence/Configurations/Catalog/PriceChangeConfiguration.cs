using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Catalog.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.Catalog;

public sealed class PriceChangeConfiguration : IEntityTypeConfiguration<PriceChange>
{
    public void Configure(EntityTypeBuilder<PriceChange> builder)
    {
        builder.ToTable("priceChanges");
        builder.HasKey(change => change.Id);
        builder.Property(change => change.Id).ValueGeneratedNever();
        builder.Property(change => change.Kind).HasConversion<string>().HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(change => change.ItemId).IsRequired();
        builder.Property(change => change.Field).HasConversion<string>().HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(change => change.OldValue).HasPrecision(12, 2);
        builder.Property(change => change.NewValue).HasPrecision(12, 2).IsRequired();
        builder.Property(change => change.ChangedBy);
        builder.Property(change => change.ChangedAt).IsRequired();

        builder.HasIndex(change => new { change.Kind, change.ItemId, change.ChangedAt }).HasDatabaseName("ix_priceChanges_item");
    }
}
