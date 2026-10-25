using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator() =>
        RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
