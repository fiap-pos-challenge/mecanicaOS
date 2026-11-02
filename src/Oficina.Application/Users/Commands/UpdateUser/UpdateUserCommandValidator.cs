using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Users.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Name).ValidUserName();
        RuleFor(command => command.Role).ValidRole();
    }
}
