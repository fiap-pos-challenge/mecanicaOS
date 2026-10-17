using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.Settings.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Quotes;

public sealed class EstimateConfiguration : IEntityTypeConfiguration<Estimate>
{
    public void Configure(EntityTypeBuilder<Estimate> builder)
    {
        builder.ToTable("estimates");
        builder.HasKey(estimate => estimate.Id);
        builder.Property(estimate => estimate.Id).ValueGeneratedNever();
        builder.Ignore(estimate => estimate.DomainEvents);
        builder.Property(estimate => estimate.Number).IsRequired();
        builder.Property(estimate => estimate.Version).IsRequired();
        builder.Property(estimate => estimate.Status).HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(estimate => estimate.DiscountMode).HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(estimate => estimate.Lines).HasJsonConversion().IsRequired();
        builder.Property(estimate => estimate.PaymentMethods).HasJsonConversion().IsRequired();
        builder.Property(estimate => estimate.AcceptedLineIds).HasJsonConversion().IsRequired();
        builder.Property(estimate => estimate.ManualDiscount).HasJsonObjectConversion();
        builder.Property(estimate => estimate.Approval).HasJsonObjectConversion();
        builder.Property(estimate => estimate.Delivery).HasJsonObjectConversion();
        builder.Property(estimate => estimate.DeliveryTokenHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
        builder.Property(estimate => estimate.MaxInstallments).IsRequired();
        builder.Property(estimate => estimate.RejectionReason).HasMaxLength(500);
        builder.HasOne<ServiceOrder>().WithMany().HasForeignKey(estimate => estimate.ServiceOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(estimate => new { estimate.Number, estimate.Version }).IsUnique().HasDatabaseName("ux_estimates_number_version");
        builder.HasIndex(estimate => estimate.DeliveryTokenHash).HasDatabaseName("ix_estimates_deliveryTokenHash");
    }
}

public sealed class SettingsDocumentConfiguration : IEntityTypeConfiguration<SettingsDocument>
{
    public void Configure(EntityTypeBuilder<SettingsDocument> builder)
    {
        builder.ToTable("settingsDocuments");
        builder.HasKey(document => document.Key);
        builder.Property(document => document.Key).HasMaxLength(SettingsDocument.KeyMaxLength).IsUnicode(false).ValueGeneratedNever();
        builder.Property(document => document.Json).IsRequired();
        builder.Property(document => document.UpdatedAt).IsRequired();
    }
}
