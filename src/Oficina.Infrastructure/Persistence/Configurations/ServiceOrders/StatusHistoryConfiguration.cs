using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.ServiceOrders.Entities;

namespace Oficina.Infrastructure.Persistence.Configurations.ServiceOrders;

public sealed class StatusHistoryConfiguration : IEntityTypeConfiguration<StatusHistory>
{
    public void Configure(EntityTypeBuilder<StatusHistory> builder)
    {
        builder.ToTable("serviceOrderStatusHistory");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();

        builder.Property(history => history.PreviousStatus).HasConversion<string>().HasMaxLength(30).IsUnicode(false);
        builder.Property(history => history.NewStatus).HasConversion<string>().HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(history => history.Note);
        builder.Property(history => history.ChangedAt).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(history => history.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
