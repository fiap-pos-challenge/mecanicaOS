using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Team.Queries.GetTeamProfile;

public sealed class GetTeamProfileQueryValidator : AbstractValidator<GetTeamProfileQuery>
{
    public GetTeamProfileQueryValidator() => RuleFor(query => query.UserId).NotEmpty().WithError(CommonErrors.RequiredField);
}
