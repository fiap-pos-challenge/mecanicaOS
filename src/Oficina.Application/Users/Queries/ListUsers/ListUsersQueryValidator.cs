using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Identity.Errors;

namespace Oficina.Application.Users.Queries.ListUsers;

public sealed class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersQueryValidator()
    {
        RuleFor(query => query.Page).ValidPage();
        RuleFor(query => query.PageSize).ValidPageSize();
        RuleFor(query => query.Role).IsInEnum().WithError(IdentityErrors.InvalidRole);
        RuleFor(query => query.Status).IsInEnum().WithError(CommonErrors.ValidationFailed);
    }
}
