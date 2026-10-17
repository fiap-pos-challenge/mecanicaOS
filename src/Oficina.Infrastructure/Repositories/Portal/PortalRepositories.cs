using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Documents.Entities;
using Oficina.Domain.Documents.Repositories;
using Oficina.Domain.Portal.Entities;
using Oficina.Domain.Portal.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Portal;

public sealed class PortalRepository(OficinaDbContext context) : IPortalRepository
{
    public Task<PortalSession?> GetSessionAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.PortalSessions.SingleOrDefaultAsync(session => session.TokenHash == tokenHash, cancellationToken);

    public Task<PortalCharge?> GetChargeAsync(Guid id, CancellationToken cancellationToken) =>
        context.PortalCharges.SingleOrDefaultAsync(charge => charge.Id == id, cancellationToken);

    public Task<PortalCharge?> GetChargeByTxIdAsync(string txId, CancellationToken cancellationToken) =>
        context.PortalCharges.SingleOrDefaultAsync(charge => charge.TxId == txId, cancellationToken);

    public Task<DateTimeOffset?> PaidAtAsync(Guid serviceOrderId, CancellationToken cancellationToken) =>
        context.PortalCharges.Where(charge => charge.ServiceOrderId == serviceOrderId && charge.Status == "Paid").MaxAsync(charge => charge.PaidAt, cancellationToken);

    public void Add(PortalSession session) => context.PortalSessions.Add(session);

    public void Add(PortalCharge charge) => context.PortalCharges.Add(charge);
}

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
