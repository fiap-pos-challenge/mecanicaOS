using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Infrastructure.Messaging.Outbox;

namespace Oficina.Infrastructure.Persistence.Configurations.Messaging;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();

        builder.Property(message => message.Type).HasMaxLength(OutboxMessage.TypeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.OccurredAt).IsRequired();
        builder.Property(message => message.Attempts).IsRequired();
        builder.Property(message => message.Error).HasMaxLength(OutboxMessage.ErrorMaxLength);

        builder.HasIndex(message => message.ProcessedAt).HasDatabaseName("ix_outboxMessages_processedAt");
    }
}
