using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.GetWorkshopSettings;

public sealed class GetWorkshopSettingsQueryHandler(SettingsStore store, ISettingsQueries queries, TimeProvider timeProvider) : IRequestHandler<GetWorkshopSettingsQuery, Result<WorkshopSettings>>
{
    public async Task<Result<WorkshopSettings>> Handle(GetWorkshopSettingsQuery query, CancellationToken cancellationToken)
    {
        var workshop = await store.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(timeProvider.GetUtcNow()), cancellationToken);
        return workshop with { Plan = workshop.Plan with { Users = await queries.ActiveUsersAsync(cancellationToken) } };
    }
}
