using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Identity.Commands.Reauthenticate;

public sealed class ReauthenticateCommandValidator : AbstractValidator<ReauthenticateCommand>
{
    public ReauthenticateCommandValidator() =>
        RuleFor(command => command)
            .Must(command => !string.IsNullOrWhiteSpace(command.Password) || !string.IsNullOrWhiteSpace(command.Code))
            .WithError(CommonErrors.RequiredField);
}
