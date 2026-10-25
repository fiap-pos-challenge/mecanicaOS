using Microsoft.EntityFrameworkCore;
using Oficina.Application.Jobs;
using Oficina.Application.Jobs.Models;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Jobs;

public sealed class BackgroundJobRepository(OficinaDbContext context) : IBackgroundJobRepository
{
    public Task<BackgroundJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.BackgroundJobs.SingleOrDefaultAsync(job => job.Id == id, cancellationToken);

    public void Add(BackgroundJob job) => context.BackgroundJobs.Add(job);
}
