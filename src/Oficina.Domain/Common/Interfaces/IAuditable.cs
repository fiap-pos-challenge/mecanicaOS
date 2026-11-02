namespace Oficina.Domain.Common.Interfaces;

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }

    DateTimeOffset? UpdatedAt { get; }

    void MarkCreated(DateTimeOffset now);

    void MarkUpdated(DateTimeOffset now);
}
