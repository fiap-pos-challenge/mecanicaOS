using MediatR;
using Oficina.Application.Notifications.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Notifications.Queries.ListNotifications;

public sealed record ListNotificationsQuery : IRequest<Result<NotificationListDto>>;
