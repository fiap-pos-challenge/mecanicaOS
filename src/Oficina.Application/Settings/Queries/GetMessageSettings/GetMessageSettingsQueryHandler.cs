using MediatR;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.GetMessageSettings;

public sealed class GetMessageSettingsQueryHandler(SettingsStore store) : IRequestHandler<GetMessageSettingsQuery, Result<MessageSettings>>
{
    public async Task<Result<MessageSettings>> Handle(GetMessageSettingsQuery query, CancellationToken cancellationToken) =>
        await store.GetAsync(SettingsKeys.Messages, SettingsDefaults.Messages, cancellationToken);
}
