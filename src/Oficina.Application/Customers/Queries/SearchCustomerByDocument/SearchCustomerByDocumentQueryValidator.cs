using FluentValidation;
using Oficina.Application.Customers.Validation;

namespace Oficina.Application.Customers.Queries.SearchCustomerByDocument;

public sealed class SearchCustomerByDocumentQueryValidator : AbstractValidator<SearchCustomerByDocumentQuery>
{
    public SearchCustomerByDocumentQueryValidator() =>
        RuleFor(query => query.Document).ValidDocument();
}
