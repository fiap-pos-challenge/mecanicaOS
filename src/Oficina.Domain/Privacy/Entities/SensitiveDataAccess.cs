using Oficina.Domain.Privacy.Enums;

namespace Oficina.Domain.Privacy.Entities;

public sealed class SensitiveDataAccess
{
    public const int TraceIdMaxLength = 64;

    private SensitiveDataAccess(Guid id, Guid userId, Guid customerId, SensitiveField field, AccessReason reason, string? traceId, DateTimeOffset occurredAt)
    {
        Id = id;
        UserId = userId;
        CustomerId = customerId;
        Field = field;
        Reason = reason;
        TraceId = traceId;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private init; }

    public Guid UserId { get; private init; }

    public Guid CustomerId { get; private init; }

    public SensitiveField Field { get; private init; }

    public AccessReason Reason { get; private init; }

    public string? TraceId { get; private init; }

    public DateTimeOffset OccurredAt { get; private init; }

    public static SensitiveDataAccess Record(Guid userId, Guid customerId, SensitiveField field, AccessReason reason, string? traceId, DateTimeOffset now) =>
        new(Guid.CreateVersion7(), userId, customerId, field, reason, traceId is { Length: > TraceIdMaxLength } ? traceId[..TraceIdMaxLength] : traceId, now);
}
