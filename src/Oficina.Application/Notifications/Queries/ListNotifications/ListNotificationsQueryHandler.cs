using MediatR;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Notifications.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Notifications;

namespace Oficina.Application.Notifications.Queries.ListNotifications;

public sealed class ListNotificationsQueryHandler(INotificationRepository notifications, NotificationPreferences preferences, ICurrentUser currentUser)
    : IRequestHandler<ListNotificationsQuery, Result<NotificationListDto>>
{
    public const int ListSize = 50;

    public async Task<Result<NotificationListDto>> Handle(ListNotificationsQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId.GetValueOrDefault();
        var hidden = (await preferences.OfAsync(userId, cancellationToken)).Where(item => !item.InApp).Select(item => item.Type).ToHashSet(StringComparer.Ordinal);
        var recent = (await notifications.RecentAsync(ListSize, cancellationToken)).Where(item => !hidden.Contains(item.Type)).ToList();
        var read = await notifications.ReadIdsAsync(userId, recent.Select(item => item.Id).ToList(), cancellationToken);
        return new NotificationListDto(recent.Select(item => new NotificationDto(item.Id, item.Type, item.Title, item.Body, item.CreatedAt, item.Link, item.AiSuggested, read.Contains(item.Id))).ToList());
    }
}
