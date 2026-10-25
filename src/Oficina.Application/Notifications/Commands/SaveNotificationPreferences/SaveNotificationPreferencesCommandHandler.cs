using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Notifications.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Notifications;

namespace Oficina.Application.Notifications.Commands.SaveNotificationPreferences;

public sealed class SaveNotificationPreferencesCommandHandler(NotificationPreferences preferences, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    : IRequestHandler<SaveNotificationPreferencesCommand, Result<NotificationPreferenceListDto>>
{
    public async Task<Result<NotificationPreferenceListDto>> Handle(SaveNotificationPreferencesCommand command, CancellationToken cancellationToken)
    {
        if (command.Items is not { } items || items.Any(item => !NotificationTypes.All.Contains(item.Type)) || items.Select(item => item.Type).Distinct(StringComparer.Ordinal).Count() != items.Count)
        {
            return NotificationErrors.InvalidPreferences;
        }

        await preferences.SaveAsync(currentUser.UserId.GetValueOrDefault(), items, cancellationToken);
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } error ? error : new NotificationPreferenceListDto(items);
    }
}
