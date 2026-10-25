using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Customers.Entities;

namespace Oficina.Application.Customers.Queries.QueryCustomers;

public sealed class QueryCustomersQueryValidator : AbstractValidator<QueryCustomersQuery>
{
    public QueryCustomersQueryValidator()
    {
        RuleFor(query => query.Term).MaximumLength(Customer.NameMaxLength).WithError(CommonErrors.InvalidSearchTerm);
        RuleFor(query => query.DocumentType).IsInEnum().WithError(CommonErrors.InvalidSearchTerm);
        RuleFor(query => query.Sort).IsInEnum().WithError(CommonErrors.InvalidSearchTerm);
        RuleFor(query => query.Direction).IsInEnum().WithError(CommonErrors.InvalidSearchTerm);
        RuleFor(query => query.Page).ValidPage();
        RuleFor(query => query.PageSize).ValidPageSize();
    }
}
