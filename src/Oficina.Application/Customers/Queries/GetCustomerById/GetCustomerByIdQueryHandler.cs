using MediatR;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;

namespace Oficina.Application.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdQueryHandler(ICustomerQueries queries) : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery query, CancellationToken cancellationToken)
    {
        if (await queries.GetByIdAsync(query.Id, cancellationToken) is not { } customer)
        {
            return CustomerErrors.NotFound;
        }

        return customer;
    }
}
