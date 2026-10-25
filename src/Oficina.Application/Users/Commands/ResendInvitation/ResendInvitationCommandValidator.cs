using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Users.Commands.ResendInvitation;

public sealed class ResendInvitationCommandValidator : AbstractValidator<ResendInvitationCommand>
{
    public ResendInvitationCommandValidator() =>
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
