using System.ComponentModel.DataAnnotations;

namespace Oficina.Infrastructure.Messaging.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    [Range(1, 3600)]
    public int PollingIntervalSeconds { get; set; }

    [Range(1, 500)]
    public int BatchSize { get; set; }

    [Range(1, 20)]
    public int MaxAttempts { get; set; }
}
