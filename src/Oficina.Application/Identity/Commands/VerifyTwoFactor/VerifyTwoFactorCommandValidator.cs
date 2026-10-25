using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Identity.Commands.VerifyTwoFactor;

public sealed class VerifyTwoFactorCommandValidator : AbstractValidator<VerifyTwoFactorCommand>
{
    public VerifyTwoFactorCommandValidator()
    {
        RuleFor(command => command.ChallengeId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Code).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
