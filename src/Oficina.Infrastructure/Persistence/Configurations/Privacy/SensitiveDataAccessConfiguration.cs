using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Privacy.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.Privacy;

public sealed class SensitiveDataAccessConfiguration : IEntityTypeConfiguration<SensitiveDataAccess>
{
    public void Configure(EntityTypeBuilder<SensitiveDataAccess> builder)
    {
        builder.ToTable("sensitiveDataAccess");
        builder.HasKey(access => access.Id);
        builder.Property(access => access.Id).ValueGeneratedNever();
        builder.Property(access => access.UserId).IsRequired();
        builder.Property(access => access.CustomerId).IsRequired();
        builder.Property(access => access.Field).HasConversion<string>().HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(access => access.Reason).HasConversion<string>().HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(access => access.TraceId).HasMaxLength(SensitiveDataAccess.TraceIdMaxLength).IsUnicode(false);
        builder.Property(access => access.OccurredAt).IsRequired();

        builder.HasIndex(access => new { access.CustomerId, access.OccurredAt }).HasDatabaseName("ix_sensitiveDataAccess_customer");
    }
}
