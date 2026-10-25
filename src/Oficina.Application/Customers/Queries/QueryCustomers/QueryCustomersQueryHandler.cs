using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Customers.Queries.QueryCustomers;

public sealed class QueryCustomersQueryHandler(ICustomerQueries queries)
    : IRequestHandler<QueryCustomersQuery, Result<PagedResult<CustomerDto>>>
{
    public async Task<Result<PagedResult<CustomerDto>>> Handle(QueryCustomersQuery query, CancellationToken cancellationToken)
    {
        var term = string.IsNullOrWhiteSpace(query.Term) ? null : query.Term.Trim();
        var document = term is null ? null : Document.Create(term);
        var filter = document is { IsSuccess: true }
            ? new CustomerFilter(null, document.Value, query.DocumentType, query.Sort, query.Direction)
            : new CustomerFilter(term, null, query.DocumentType, query.Sort, query.Direction);
        return await queries.QueryAsync(filter, new PageRequest(query.Page, query.PageSize), cancellationToken);
    }
}
