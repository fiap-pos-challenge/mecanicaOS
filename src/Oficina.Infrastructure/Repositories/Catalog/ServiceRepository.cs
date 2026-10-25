using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Catalog;

public sealed class ServiceRepository(OficinaDbContext context) : IServiceRepository
{
    public Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Services.SingleOrDefaultAsync(service => service.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Service>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ids.Count == 0 ? [] : await context.Services.Where(service => ids.Contains(service.Id)).ToListAsync(cancellationToken);

    public void Add(Service service) => context.Services.Add(service);
}
