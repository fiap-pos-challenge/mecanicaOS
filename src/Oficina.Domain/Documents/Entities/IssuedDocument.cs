namespace Oficina.Domain.Documents.Entities;

public sealed class IssuedDocument
{
    public const int CodeMaxLength = 30;

    private IssuedDocument(Guid id, string code, string kind, Guid serviceOrderId, DateTimeOffset issuedAt, DocumentSnapshot snapshot)
    {
        Id = id;
        Code = code;
        Kind = kind;
        ServiceOrderId = serviceOrderId;
        IssuedAt = issuedAt;
        Snapshot = snapshot;
    }

    public Guid Id { get; private init; }

    public string Code { get; private init; }

    public string Kind { get; private init; }

    public Guid ServiceOrderId { get; private init; }

    public DateTimeOffset IssuedAt { get; private init; }

    public DocumentSnapshot Snapshot { get; private init; }

    public static IssuedDocument Issue(string code, string kind, Guid serviceOrderId, DateTimeOffset now, DocumentSnapshot snapshot) =>
        new(Guid.CreateVersion7(), code, kind, serviceOrderId, now, snapshot);
}
