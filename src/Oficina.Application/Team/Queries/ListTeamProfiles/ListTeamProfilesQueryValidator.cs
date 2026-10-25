using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Team.Errors;

namespace Oficina.Application.Team.Queries.ListTeamProfiles;

public sealed class ListTeamProfilesQueryValidator : AbstractValidator<ListTeamProfilesQuery>
{
    public const int MaxUsers = 500;

    public ListTeamProfilesQueryValidator() =>
        RuleFor(query => query.UserIds).Must(ids => ids is not null && ids.Count <= MaxUsers).WithError(TeamErrors.TooManyUsers);
}
