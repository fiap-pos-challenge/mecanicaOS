using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Queries.ListPackages;

public sealed class ListPackagesQueryValidator : AbstractValidator<ListPackagesQuery>
{
    public ListPackagesQueryValidator()
    {
        RuleFor(query => query.Page).ValidPage();
        RuleFor(query => query.PageSize).ValidPageSize();
        RuleFor(query => query.Term).MaximumLength(120).WithError(CommonErrors.InvalidSearchTerm);
    }
}
