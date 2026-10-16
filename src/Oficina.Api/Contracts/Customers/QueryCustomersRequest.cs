using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Queries;
using Oficina.Domain.Customers.Enums;

namespace Oficina.Api.Contracts.Customers;

public sealed record QueryCustomersRequest(
    string? Term,
    DocumentType? DocumentType,
    CustomerSort? Sort,
    SortDirection? Direction,
    int? Page,
    int? PageSize);
