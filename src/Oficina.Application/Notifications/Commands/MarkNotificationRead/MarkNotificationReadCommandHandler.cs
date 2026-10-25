using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Notifications;

namespace Oficina.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler(INotificationRepository notifications, ICurrentUser currentUser, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        if (await notifications.GetByIdAsync(command.Id, cancellationToken) is null)
        {
            return NotificationErrors.NotFound;
        }

        var userId = currentUser.UserId.GetValueOrDefault();
        if ((await notifications.ReadIdsAsync(userId, [command.Id], cancellationToken)).Count == 0)
        {
            notifications.Add(NotificationRead.Mark(command.Id, userId, timeProvider.GetUtcNow()));
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
