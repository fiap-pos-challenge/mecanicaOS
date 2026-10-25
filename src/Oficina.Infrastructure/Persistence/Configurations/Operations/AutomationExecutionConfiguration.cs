using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Automations;

namespace Oficina.Infrastructure.Persistence.Configurations.Operations;

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
