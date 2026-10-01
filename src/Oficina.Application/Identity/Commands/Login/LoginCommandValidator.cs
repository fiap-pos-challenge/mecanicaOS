using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Identity.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Password).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
