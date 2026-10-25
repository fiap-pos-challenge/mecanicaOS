using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Quotes;

public sealed class EstimateConfiguration : IEntityTypeConfiguration<Estimate>
{
    private const string LinesField = "_lines";
    private const string PaymentMethodsField = "_paymentMethods";
    private const string AcceptedLineIdsField = "_acceptedLineIds";

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
        builder.Ignore(estimate => estimate.Lines);
        builder.Ignore(estimate => estimate.PaymentMethods);
        builder.Ignore(estimate => estimate.AcceptedLineIds);
        builder.Ignore(estimate => estimate.EffectiveLines);
        builder.Ignore(estimate => estimate.IsPortalLocked);
        builder.Ignore(estimate => estimate.PortalAttemptsLeft);
        builder.Ignore(estimate => estimate.IsPartiallyApproved);
        builder.Property<List<EstimateLine>>(LinesField).HasField(LinesField).HasColumnName("lines").HasJsonConversion().IsRequired();
        builder.Property<List<string>>(PaymentMethodsField).HasField(PaymentMethodsField).HasColumnName("paymentMethods").HasJsonConversion().IsRequired();
        builder.Property<List<Guid>>(AcceptedLineIdsField).HasField(AcceptedLineIdsField).HasColumnName("acceptedLineIds").HasJsonConversion().IsRequired();
        builder.Property<byte[]>(ConcurrencyToken.RowVersion).IsRowVersion().IsRequired();
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
