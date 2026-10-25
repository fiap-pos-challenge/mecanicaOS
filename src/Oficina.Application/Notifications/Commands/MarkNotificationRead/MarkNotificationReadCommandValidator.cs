using FluentValidation;
using Oficina.Domain.Notifications;

namespace Oficina.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithErrorCode(NotificationErrors.NotFound.Code).WithMessage(NotificationErrors.NotFound.Message);
    }
}
