using Oficina.Domain.Common.Interfaces;

namespace Oficina.Domain.Common.Entities;

public abstract class Entity : IAuditable, ISoftDeletable
{
    protected Entity(Guid id) => Id = id;

    public Guid Id { get; private init; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    void IAuditable.MarkCreated(DateTimeOffset now) => CreatedAt = now;

    void IAuditable.MarkUpdated(DateTimeOffset now) => UpdatedAt = now;

    void ISoftDeletable.MarkDeleted(DateTimeOffset now) => DeletedAt = now;
}
