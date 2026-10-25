using MediatR;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Notifications.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Notifications.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryHandler(NotificationPreferences preferences, ICurrentUser currentUser)
    : IRequestHandler<GetNotificationPreferencesQuery, Result<NotificationPreferenceListDto>>
{
    public async Task<Result<NotificationPreferenceListDto>> Handle(GetNotificationPreferencesQuery query, CancellationToken cancellationToken) =>
        new NotificationPreferenceListDto(await preferences.OfAsync(currentUser.UserId.GetValueOrDefault(), cancellationToken));
}
