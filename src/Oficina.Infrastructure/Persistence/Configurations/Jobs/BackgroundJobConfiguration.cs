using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Jobs.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.Jobs;

public sealed class BackgroundJobConfiguration : IEntityTypeConfiguration<BackgroundJob>
{
    public void Configure(EntityTypeBuilder<BackgroundJob> builder)
    {
        builder.ToTable("backgroundJobs");
        builder.HasKey(job => job.Id);
        builder.Property(job => job.Id).ValueGeneratedNever();
        builder.Property(job => job.Kind).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(job => job.Status).HasMaxLength(12).IsUnicode(false).IsRequired();
        builder.Property(job => job.CreatedAt).IsRequired();
        builder.Property(job => job.FileName).HasMaxLength(120);
        builder.Property(job => job.ResultJson);
    }
}
