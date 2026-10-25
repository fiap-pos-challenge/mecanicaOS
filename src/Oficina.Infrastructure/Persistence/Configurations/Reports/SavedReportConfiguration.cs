using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Reports;

namespace Oficina.Infrastructure.Persistence.Configurations.Reports;

public sealed class SavedReportConfiguration : IEntityTypeConfiguration<SavedReport>
{
    public void Configure(EntityTypeBuilder<SavedReport> builder)
    {
        builder.ToTable("savedReports");
        builder.HasKey(report => report.Id);
        builder.Property(report => report.Id).ValueGeneratedNever();
        builder.Property(report => report.Name).HasMaxLength(SavedReport.NameMaxLength).IsRequired();
        builder.Property(report => report.DefinitionJson).HasMaxLength(1000).IsRequired();
        builder.Property(report => report.ScheduleJson).HasMaxLength(2000);
        builder.Property(report => report.UpdatedAt).IsRequired();
        builder.HasIndex(report => report.Name).IsUnique().HasDatabaseName("ux_savedReports_name");
    }
}
