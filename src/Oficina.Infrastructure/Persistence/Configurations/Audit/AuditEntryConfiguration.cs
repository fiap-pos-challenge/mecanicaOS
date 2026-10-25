using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Audit.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.Audit;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("auditEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();
        builder.Property(entry => entry.OccurredAt).IsRequired();
        builder.Property(entry => entry.ActorName).HasMaxLength(AuditEntry.TextMaxLength).IsRequired();
        builder.Property(entry => entry.Area).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(entry => entry.Action).HasMaxLength(AuditEntry.TextMaxLength).IsRequired();
        builder.Property(entry => entry.Target).HasMaxLength(AuditEntry.TextMaxLength).IsRequired();
        builder.Property(entry => entry.Detail).HasMaxLength(AuditEntry.TextMaxLength).IsRequired();
        builder.HasIndex(entry => entry.OccurredAt).HasDatabaseName("ix_auditEntries_occurredAt");
    }
}
