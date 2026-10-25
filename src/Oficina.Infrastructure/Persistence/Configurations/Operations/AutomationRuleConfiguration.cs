using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Automations;

namespace Oficina.Infrastructure.Persistence.Configurations.Operations;

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
