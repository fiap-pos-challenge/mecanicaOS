using System.Text.Json;
using Oficina.Domain.Common.Events;

namespace Oficina.Infrastructure.Messaging.Outbox;

public sealed class OutboxMessage
{
    public const int TypeMaxLength = 200;
    public const int ErrorMaxLength = 2000;
    public const string UnknownEventTypeError = "UnknownEventType";

    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private OutboxMessage(Guid id, string type, string payload, DateTimeOffset occurredAt, int attempts)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAt = occurredAt;
        Attempts = attempts;
    }

    public Guid Id { get; private init; }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? Error { get; private set; }

    public static OutboxMessage Create(IDomainEvent domainEvent) =>
        new(
            Guid.CreateVersion7(),
            domainEvent.GetType().Name,
            JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
            domainEvent.OccurredAt,
            0);

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        Error = null;
    }

    public void RecordFailure(string error)
    {
        Attempts++;
        Error = error.Length > ErrorMaxLength ? error[..ErrorMaxLength] : error;
    }
}
