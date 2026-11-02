using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Customers.Queries.ListCustomers;

public sealed class ListCustomersQueryHandler(ICustomerQueries queries)
    : IRequestHandler<ListCustomersQuery, Result<PagedResult<CustomerDto>>>
{
    public async Task<Result<PagedResult<CustomerDto>>> Handle(ListCustomersQuery query, CancellationToken cancellationToken) =>
        await queries.ListAsync(new PageRequest(query.Page, query.PageSize), cancellationToken);
}
