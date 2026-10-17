using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Automations;
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

public sealed class AutomationRuleConfiguration : IEntityTypeConfiguration<AutomationRule>
{
    public void Configure(EntityTypeBuilder<AutomationRule> builder)
    {
        builder.ToTable("automationRules");
        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Id).ValueGeneratedNever();
        builder.Property(rule => rule.Trigger).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(rule => rule.DefinitionJson).HasMaxLength(4000).IsRequired();
        builder.Property(rule => rule.CreatedAt).IsRequired();
    }
}

public sealed class AutomationExecutionConfiguration : IEntityTypeConfiguration<AutomationExecution>
{
    public void Configure(EntityTypeBuilder<AutomationExecution> builder)
    {
        builder.ToTable("automationExecutions");
        builder.HasKey(execution => execution.Id);
        builder.Property(execution => execution.Id).ValueGeneratedNever();
        builder.Property(execution => execution.Trigger).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(execution => execution.DedupeKey).HasMaxLength(120).IsUnicode(false).IsRequired();
        builder.Property(execution => execution.Status).HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(execution => execution.Summary).HasMaxLength(AutomationExecution.TextMaxLength).IsRequired();
        builder.Property(execution => execution.Error).HasMaxLength(AutomationExecution.TextMaxLength);
        builder.Property(execution => execution.DueAt).IsRequired();
        builder.Property(execution => execution.CreatedAt).IsRequired();
        builder.HasOne<AutomationRule>().WithMany().HasForeignKey(execution => execution.RuleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(execution => new { execution.RuleId, execution.DedupeKey }).IsUnique().HasDatabaseName("ux_automationExecutions_rule_dedupe");
        builder.HasIndex(execution => new { execution.Status, execution.DueAt }).HasDatabaseName("ix_automationExecutions_status_dueAt");
    }
}
