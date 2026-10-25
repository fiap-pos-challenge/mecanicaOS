using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Notifications.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid Id) : IRequest<Result>;
