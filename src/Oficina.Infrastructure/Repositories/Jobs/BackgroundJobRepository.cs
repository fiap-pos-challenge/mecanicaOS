using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Jobs.Entities;
using Oficina.Domain.Jobs.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Jobs;

public sealed class BackgroundJobRepository(OficinaDbContext context) : IBackgroundJobRepository
{
    public Task<BackgroundJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.BackgroundJobs.SingleOrDefaultAsync(job => job.Id == id, cancellationToken);

    public void Add(BackgroundJob job) => context.BackgroundJobs.Add(job);
}
