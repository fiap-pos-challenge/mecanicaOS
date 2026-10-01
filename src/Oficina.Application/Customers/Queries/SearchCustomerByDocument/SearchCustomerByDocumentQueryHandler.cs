using MediatR;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Customers.Queries.SearchCustomerByDocument;

public sealed class SearchCustomerByDocumentQueryHandler(ICustomerQueries queries)
    : IRequestHandler<SearchCustomerByDocumentQuery, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(SearchCustomerByDocumentQuery query, CancellationToken cancellationToken)
    {
        var document = Document.Create(query.Document);
        if (document.Error is { } error)
        {
            return error;
        }

        if (await queries.GetByDocumentAsync(document.Value, cancellationToken) is not { } customer)
        {
            return CustomerErrors.NotFound;
        }

        return customer;
    }
}
