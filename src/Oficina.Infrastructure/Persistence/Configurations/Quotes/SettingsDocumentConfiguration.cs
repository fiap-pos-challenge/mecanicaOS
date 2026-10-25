using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Settings.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.Quotes;

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
