using FluentValidation;
using Oficina.Application.Users.Validation;

namespace Oficina.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Name).ValidUserName();
        RuleFor(command => command.Email).ValidEmail();
        RuleFor(command => command.Password).StrongPassword();
        RuleFor(command => command.Role).ValidRole();
    }
}
