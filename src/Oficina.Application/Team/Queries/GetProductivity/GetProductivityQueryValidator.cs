using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Team.Queries.ListTeamProfiles;
using Oficina.Domain.Team.Errors;

namespace Oficina.Application.Team.Queries.GetProductivity;

public sealed class GetProductivityQueryValidator : AbstractValidator<GetProductivityQuery>
{
    public GetProductivityQueryValidator()
    {
        RuleFor(query => query.UserIds).Must(ids => ids is not null && ids.Count <= ListTeamProfilesQueryValidator.MaxUsers).WithError(TeamErrors.TooManyUsers);
        RuleFor(query => query.PeriodDays).Must(days => days is 30 or 90).WithError(TeamErrors.InvalidPeriod);
    }
}
