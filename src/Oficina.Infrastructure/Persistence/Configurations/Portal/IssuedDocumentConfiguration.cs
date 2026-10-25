using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Documents.Entities;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Infrastructure.Persistence.Converters;

namespace Oficina.Infrastructure.Persistence.Configurations.Portal;

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
