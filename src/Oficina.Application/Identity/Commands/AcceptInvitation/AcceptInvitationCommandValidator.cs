using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Users.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Identity.Commands.AcceptInvitation;

public sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty().MaximumLength(InvitationToken.MaxLength).WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Password).StrongPassword();
    }
}
