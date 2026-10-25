using Oficina.Application.Jobs.Models;

namespace Oficina.Application.Jobs;

public interface IBackgroundJobRepository
{
    Task<BackgroundJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(BackgroundJob job);
}
