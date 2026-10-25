using Oficina.Domain.Documents.Entities;

namespace Oficina.Domain.Documents.Repositories;

public interface IDocumentRepository
{
    Task<IssuedDocument?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<IssuedDocument>> ListByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken);

    Task<bool> HasAsync(Guid serviceOrderId, string kind, CancellationToken cancellationToken);

    void Add(IssuedDocument document);
}
