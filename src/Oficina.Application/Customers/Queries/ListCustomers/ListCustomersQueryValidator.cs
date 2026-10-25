using FluentValidation;
using Oficina.Application.Common.Validation;

namespace Oficina.Application.Customers.Queries.ListCustomers;

public sealed class ListCustomersQueryValidator : AbstractValidator<ListCustomersQuery>
{
    public ListCustomersQueryValidator()
    {
        RuleFor(query => query.Page).ValidPage();
        RuleFor(query => query.PageSize).ValidPageSize();
    }
}
