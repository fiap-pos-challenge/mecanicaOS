using Oficina.Domain.Jobs.Entities;

namespace Oficina.Domain.Jobs.Repositories;

public interface IBackgroundJobRepository
{
    Task<BackgroundJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(BackgroundJob job);
}
