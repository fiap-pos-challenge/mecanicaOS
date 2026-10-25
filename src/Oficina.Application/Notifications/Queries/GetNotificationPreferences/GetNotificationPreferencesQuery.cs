using MediatR;
using Oficina.Application.Notifications.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Notifications.Queries.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery : IRequest<Result<NotificationPreferenceListDto>>;
