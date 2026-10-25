using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Notifications;

namespace Oficina.Infrastructure.Persistence.Configurations.Operations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Id).ValueGeneratedNever();
        builder.Property(notification => notification.Type).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(notification => notification.Title).HasMaxLength(Notification.TitleMaxLength).IsRequired();
        builder.Property(notification => notification.Body).HasMaxLength(Notification.BodyMaxLength).IsRequired();
        builder.Property(notification => notification.Link).HasMaxLength(120).IsUnicode(false);
        builder.Property(notification => notification.DedupeKey).HasMaxLength(80).IsUnicode(false);
        builder.Property(notification => notification.CreatedAt).IsRequired();
        builder.HasIndex(notification => notification.CreatedAt).HasDatabaseName("ix_notifications_createdAt");
        builder.HasIndex(notification => notification.DedupeKey).HasDatabaseName("ix_notifications_dedupeKey");
    }
}
