using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Dashboard;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Dashboard;

public sealed class DashboardActionRepository(OficinaDbContext context) : IDashboardActionRepository
{
    public async Task<IReadOnlyList<string>> HandledSinceAsync(DateTimeOffset since, CancellationToken cancellationToken) =>
        await context.DashboardActions.AsNoTracking().Where(action => action.OccurredAt >= since).Select(action => action.ItemId).Distinct().ToListAsync(cancellationToken);

    public void Add(DashboardAction action) => context.DashboardActions.Add(action);
}
