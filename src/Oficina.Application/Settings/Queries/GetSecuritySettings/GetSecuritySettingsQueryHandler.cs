using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.GetSecuritySettings;

public sealed class GetSecuritySettingsQueryHandler(SettingsStore store) : IRequestHandler<GetSecuritySettingsQuery, Result<SecuritySettings>>
{
    public async Task<Result<SecuritySettings>> Handle(GetSecuritySettingsQuery query, CancellationToken cancellationToken) =>
        await store.GetAsync(SettingsKeys.Security, SettingsDefaults.Security, cancellationToken);
}
