using FluentValidation;
using Oficina.Application.Users.Validation;

namespace Oficina.Application.Users.Queries.SearchUserByEmail;

public sealed class SearchUserByEmailQueryValidator : AbstractValidator<SearchUserByEmailQuery>
{
    public SearchUserByEmailQueryValidator() =>
        RuleFor(query => query.Email).ValidEmail();
}
