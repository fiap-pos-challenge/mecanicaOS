using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Customers.Queries.ListCustomers;

public sealed record ListCustomersQuery(int Page, int PageSize) : IRequest<Result<PagedResult<CustomerDto>>>;
