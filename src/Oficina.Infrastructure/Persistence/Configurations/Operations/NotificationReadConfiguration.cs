using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Notifications;

namespace Oficina.Infrastructure.Persistence.Configurations.Operations;

public sealed class NotificationReadConfiguration : IEntityTypeConfiguration<NotificationRead>
{
    public void Configure(EntityTypeBuilder<NotificationRead> builder)
    {
        builder.ToTable("notificationReads");
        builder.HasKey(read => new { read.NotificationId, read.UserId });
        builder.Property(read => read.ReadAt).IsRequired();
        builder.HasOne<Notification>().WithMany().HasForeignKey(read => read.NotificationId).OnDelete(DeleteBehavior.Cascade);
    }
}
