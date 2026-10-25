using Oficina.Domain.Portal.Entities;

namespace Oficina.Domain.Portal.Repositories;

public interface IPortalRepository
{
    Task<PortalSession?> GetSessionAsync(string tokenHash, CancellationToken cancellationToken);

    Task<PortalCharge?> GetChargeAsync(Guid id, CancellationToken cancellationToken);

    Task<PortalCharge?> GetChargeByTxIdAsync(string txId, CancellationToken cancellationToken);

    Task<DateTimeOffset?> PaidAtAsync(Guid serviceOrderId, CancellationToken cancellationToken);

    void Add(PortalSession session);

    void Add(PortalCharge charge);
}
