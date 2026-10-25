using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Dashboard;

namespace Oficina.Infrastructure.Persistence.Configurations.Dashboard;

public sealed class DashboardActionConfiguration : IEntityTypeConfiguration<DashboardAction>
{
    public void Configure(EntityTypeBuilder<DashboardAction> builder)
    {
        builder.ToTable("dashboardActions");
        builder.HasKey(action => action.Id);
        builder.Property(action => action.Id).ValueGeneratedNever();
        builder.Property(action => action.ItemId).HasMaxLength(60).IsUnicode(false).IsRequired();
        builder.Property(action => action.Action).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(action => action.OccurredAt).IsRequired();
        builder.HasIndex(action => action.OccurredAt).HasDatabaseName("ix_dashboardActions_occurredAt");
    }
}
