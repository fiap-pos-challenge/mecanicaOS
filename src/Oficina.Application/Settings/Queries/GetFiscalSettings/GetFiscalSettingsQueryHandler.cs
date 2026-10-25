using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.GetFiscalSettings;

public sealed class GetFiscalSettingsQueryHandler(SettingsStore store) : IRequestHandler<GetFiscalSettingsQuery, Result<FiscalSettings>>
{
    public async Task<Result<FiscalSettings>> Handle(GetFiscalSettingsQuery query, CancellationToken cancellationToken) =>
        await store.GetAsync(SettingsKeys.Fiscal, SettingsDefaults.Fiscal, cancellationToken);
}
