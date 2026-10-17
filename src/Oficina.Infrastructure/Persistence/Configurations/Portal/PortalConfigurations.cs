using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Documents.Entities;
using Oficina.Domain.Portal.Entities;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Portal;

public sealed class PortalSessionConfiguration : IEntityTypeConfiguration<PortalSession>
{
    public void Configure(EntityTypeBuilder<PortalSession> builder)
    {
        builder.ToTable("portalSessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).ValueGeneratedNever();
        builder.Property(session => session.TokenHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(session => session.ExpiresAt).IsRequired();
        builder.HasOne<Estimate>().WithMany().HasForeignKey(session => session.EstimateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(session => session.TokenHash).IsUnique().HasDatabaseName("ux_portalSessions_tokenHash");
    }
}

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

public sealed class IssuedDocumentConfiguration : IEntityTypeConfiguration<IssuedDocument>
{
    public void Configure(EntityTypeBuilder<IssuedDocument> builder)
    {
        builder.ToTable("issuedDocuments");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.Id).ValueGeneratedNever();
        builder.Property(document => document.Code).HasMaxLength(IssuedDocument.CodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(document => document.Kind).HasMaxLength(15).IsUnicode(false).IsRequired();
        builder.Property(document => document.IssuedAt).IsRequired();
        builder.Property(document => document.Snapshot).HasJsonValueConversion().IsRequired();
        builder.HasOne<ServiceOrder>().WithMany().HasForeignKey(document => document.ServiceOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(document => document.Code).IsUnique().HasDatabaseName("ux_issuedDocuments_code");
        builder.HasIndex(document => document.ServiceOrderId).HasDatabaseName("ix_issuedDocuments_serviceOrderId");
    }
}
