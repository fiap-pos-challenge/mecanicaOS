using FluentValidation;
using Oficina.Application.Common.Validation;

namespace Oficina.Application.Inventory.Queries.ListParts;

public sealed class ListPartsQueryValidator : AbstractValidator<ListPartsQuery>
{
    public ListPartsQueryValidator()
    {
        RuleFor(query => query.Page).ValidPage();
        RuleFor(query => query.PageSize).ValidPageSize();
    }
}
