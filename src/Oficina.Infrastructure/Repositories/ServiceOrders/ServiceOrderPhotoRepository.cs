using Microsoft.EntityFrameworkCore;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.ServiceOrders;

public sealed class ServiceOrderPhotoRepository(OficinaDbContext context) : IServiceOrderPhotoRepository
{
    public Task<bool> ExistsAsync(Guid serviceOrderId, string clientId, CancellationToken cancellationToken) =>
        context.ServiceOrderPhotos.AnyAsync(photo => photo.ServiceOrderId == serviceOrderId && photo.ClientId == clientId, cancellationToken);

    public Task<ServiceOrderPhoto?> GetAsync(Guid serviceOrderId, Guid photoId, CancellationToken cancellationToken) =>
        context.ServiceOrderPhotos.AsNoTracking().SingleOrDefaultAsync(photo => photo.ServiceOrderId == serviceOrderId && photo.Id == photoId, cancellationToken);

    public async Task<IReadOnlyList<ServiceOrderPhotoSummary>> ListSummariesAsync(IReadOnlyCollection<Guid> serviceOrderIds, CancellationToken cancellationToken) =>
        await context.ServiceOrderPhotos.AsNoTracking()
            .Where(photo => serviceOrderIds.Contains(photo.ServiceOrderId))
            .OrderBy(photo => photo.TakenAt)
            .Select(photo => new ServiceOrderPhotoSummary(photo.Id, photo.ServiceOrderId, photo.Caption, photo.TakenAt))
            .ToListAsync(cancellationToken);

    public void Add(ServiceOrderPhoto photo) => context.ServiceOrderPhotos.Add(photo);
}
