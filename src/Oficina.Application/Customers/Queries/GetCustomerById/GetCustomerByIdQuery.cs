using MediatR;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Customers.Queries.GetCustomerById;

public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<Result<CustomerDto>>;
