namespace Oficina.Application.Analytics;

public interface IAnalyticsQueries
{
    Task<IReadOnlyList<SaleFact>> SalesAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<OperationsFact> OperationsAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
