namespace Oficina.Application.Abstractions.Persistence;

public static class UnitOfWorkExtensions
{
    public static async Task CommitOrThrowAsync(this IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } error)
        {
            throw new CommitFailedException(error);
        }
    }
}
