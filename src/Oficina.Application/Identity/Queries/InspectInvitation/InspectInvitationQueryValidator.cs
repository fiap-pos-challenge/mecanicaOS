using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Identity.Invitations;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Identity.Queries.InspectInvitation;

public sealed class InspectInvitationQueryValidator : AbstractValidator<InspectInvitationQuery>
{
    public InspectInvitationQueryValidator() =>
        RuleFor(query => query.Token).NotEmpty().MaximumLength(InvitationToken.MaxLength).WithError(CommonErrors.RequiredField);
}
