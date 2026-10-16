using FluentValidation;
using Oficina.Application.Users.Validation;

namespace Oficina.Application.Users.Commands.InviteUser;

public sealed class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(command => command.Name).ValidUserName();
        RuleFor(command => command.Email).ValidEmail();
        RuleFor(command => command.Role).ValidRole();
    }
}
