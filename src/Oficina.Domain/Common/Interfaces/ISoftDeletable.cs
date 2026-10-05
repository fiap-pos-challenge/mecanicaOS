namespace Oficina.Domain.Common.Interfaces;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }

    void MarkDeleted(DateTimeOffset now);
}
