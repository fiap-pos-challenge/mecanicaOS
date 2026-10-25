using Oficina.Domain.Quotes.Entities;

namespace Oficina.Domain.Quotes.Repositories;

public interface IEstimateRepository
{
    Task<Estimate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Estimate?> GetLatestByNumberAsync(long number, CancellationToken cancellationToken);

    Task<Estimate?> GetLatestByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken);

    Task<Estimate?> GetByDeliveryTokenAsync(string tokenHash, CancellationToken cancellationToken);

    Task<bool> ExistsForServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken);

    Task<long> NextNumberAsync(CancellationToken cancellationToken);

    void Add(Estimate estimate);
}
