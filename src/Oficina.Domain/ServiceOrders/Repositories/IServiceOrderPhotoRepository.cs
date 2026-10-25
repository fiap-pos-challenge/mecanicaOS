using Oficina.Domain.ServiceOrders.Entities;

namespace Oficina.Domain.ServiceOrders.Repositories;

public interface IServiceOrderPhotoRepository
{
    Task<bool> ExistsAsync(Guid serviceOrderId, string clientId, CancellationToken cancellationToken);

    Task<ServiceOrderPhoto?> GetAsync(Guid serviceOrderId, Guid photoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceOrderPhotoSummary>> ListSummariesAsync(IReadOnlyCollection<Guid> serviceOrderIds, CancellationToken cancellationToken);

    void Add(ServiceOrderPhoto photo);
}
