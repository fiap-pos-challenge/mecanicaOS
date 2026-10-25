using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Users.Commands.ReactivateUser;

public sealed class ReactivateUserCommandValidator : AbstractValidator<ReactivateUserCommand>
{
    public ReactivateUserCommandValidator() =>
        RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
