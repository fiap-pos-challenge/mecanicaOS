using Microsoft.EntityFrameworkCore;
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
