using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Documents.Entities;
using Oficina.Domain.Documents.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Portal;

public sealed class DocumentRepository(OficinaDbContext context) : IDocumentRepository
{
    public Task<IssuedDocument?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        context.IssuedDocuments.SingleOrDefaultAsync(document => document.Code == code, cancellationToken);

    public async Task<IReadOnlyList<IssuedDocument>> ListByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken) =>
        await context.IssuedDocuments.Where(document => document.ServiceOrderId == serviceOrderId).ToListAsync(cancellationToken);

    public async Task<bool> HasAsync(Guid serviceOrderId, string kind, CancellationToken cancellationToken) =>
        context.IssuedDocuments.Local.Any(document => document.ServiceOrderId == serviceOrderId && document.Kind == kind)
        || await context.IssuedDocuments.AnyAsync(document => document.ServiceOrderId == serviceOrderId && document.Kind == kind, cancellationToken);

    public void Add(IssuedDocument document) => context.IssuedDocuments.Add(document);
}
