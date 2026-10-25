using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Identity.Commands.EnableTwoFactor;

public sealed class EnableTwoFactorCommandValidator : AbstractValidator<EnableTwoFactorCommand>
{
    public EnableTwoFactorCommandValidator() => RuleFor(command => command.Code).NotEmpty().WithError(CommonErrors.RequiredField);
}
