using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Queries.ListServices;

public sealed class ListServicesQueryValidator : AbstractValidator<ListServicesQuery>
{
    public ListServicesQueryValidator()
    {
        RuleFor(query => query.Page).ValidPage();
        RuleFor(query => query.PageSize).ValidPageSize();
        RuleFor(query => query.Term).MaximumLength(Service.NameMaxLength).WithError(CommonErrors.InvalidSearchTerm);
    }
}
