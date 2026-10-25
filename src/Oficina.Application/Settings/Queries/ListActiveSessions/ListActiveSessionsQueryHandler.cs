using MediatR;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Settings.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Settings.Queries.ListActiveSessions;

public sealed class ListActiveSessionsQueryHandler(ISettingsQueries queries, ICurrentUser currentUser, TimeProvider timeProvider) : IRequestHandler<ListActiveSessionsQuery, Result<SessionListDto>>
{
    public async Task<Result<SessionListDto>> Handle(ListActiveSessionsQuery query, CancellationToken cancellationToken) =>
        new SessionListDto(await queries.SessionsAsync(currentUser.SessionId, timeProvider.GetUtcNow(), cancellationToken));
}
