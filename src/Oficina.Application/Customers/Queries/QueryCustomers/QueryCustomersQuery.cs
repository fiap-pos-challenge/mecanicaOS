using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Enums;

namespace Oficina.Application.Customers.Queries.QueryCustomers;

public sealed record QueryCustomersQuery(
    string? Term,
    DocumentType? DocumentType,
    CustomerSort Sort,
    SortDirection Direction,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<CustomerDto>>>;
