using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Notifications.Queries.ListNotifications;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Notifications;

namespace Oficina.Application.Notifications.Commands.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadCommandHandler(INotificationRepository notifications, ICurrentUser currentUser, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    public async Task<Result> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId.GetValueOrDefault();
        var recent = await notifications.RecentAsync(ListNotificationsQueryHandler.ListSize, cancellationToken);
        var read = await notifications.ReadIdsAsync(userId, recent.Select(item => item.Id).ToList(), cancellationToken);
        foreach (var item in recent.Where(item => !read.Contains(item.Id)))
        {
            notifications.Add(NotificationRead.Mark(item.Id, userId, timeProvider.GetUtcNow()));
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
