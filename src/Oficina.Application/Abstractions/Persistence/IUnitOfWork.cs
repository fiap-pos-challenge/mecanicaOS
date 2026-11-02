using Oficina.Domain.Common.Results;

namespace Oficina.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<Result> CommitAsync(CancellationToken cancellationToken);
}
