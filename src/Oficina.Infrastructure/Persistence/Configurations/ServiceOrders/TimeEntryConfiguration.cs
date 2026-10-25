using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.ServiceOrders.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.ServiceOrders;

public sealed class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("timeEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();
        builder.Property(entry => entry.StartedAt).IsRequired();
        builder.Property(entry => entry.Minutes).IsRequired();
        builder.Ignore(entry => entry.IsRunning);
        builder.HasOne<User>().WithMany().HasForeignKey(entry => entry.MechanicId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entry => new { entry.MechanicId, entry.StartedAt }).HasDatabaseName("ix_timeEntries_mechanic");
    }
}
