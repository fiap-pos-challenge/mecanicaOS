using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Notifications.Commands.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadCommand : IRequest<Result>;
