using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.ListServices;

public sealed record ListServicesQuery(int Page, int PageSize, bool IncludeInactive, string? Term = null, CatalogStatus? Status = null) : IRequest<Result<PagedResult<ServiceDto>>>;
