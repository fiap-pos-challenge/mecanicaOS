using FluentValidation;
using Oficina.Application.Common.Validation;

namespace Oficina.Application.Catalog.Queries.ListServices;

public sealed class ListServicesQueryValidator : AbstractValidator<ListServicesQuery>
{
    public ListServicesQueryValidator()
    {
        RuleFor(query => query.Page).ValidPage();
        RuleFor(query => query.PageSize).ValidPageSize();
    }
}
