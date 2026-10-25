using MediatR;
using Oficina.Application.Notifications.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Notifications.Commands.SaveNotificationPreferences;

public sealed record SaveNotificationPreferencesCommand(IReadOnlyList<NotificationPreferenceDto>? Items) : IRequest<Result<NotificationPreferenceListDto>>;
