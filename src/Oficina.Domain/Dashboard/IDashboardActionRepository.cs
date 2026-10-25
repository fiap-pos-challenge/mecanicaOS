namespace Oficina.Domain.Dashboard;

public interface IDashboardActionRepository
{
    Task<IReadOnlyList<string>> HandledSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);

    void Add(DashboardAction action);
}
