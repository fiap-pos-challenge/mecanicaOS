using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Queries.ListParts;

public sealed class ListPartsQueryValidator : AbstractValidator<ListPartsQuery>
{
    public ListPartsQueryValidator()
    {
        RuleFor(query => query.Page).ValidPage();
        RuleFor(query => query.PageSize).ValidPageSize();
        RuleFor(query => query.Term).MaximumLength(120).WithError(CommonErrors.InvalidSearchTerm);
    }
}
