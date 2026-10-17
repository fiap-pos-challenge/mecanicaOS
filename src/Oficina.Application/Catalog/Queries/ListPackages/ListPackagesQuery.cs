using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.ListPackages;

public sealed record ListPackagesQuery(int Page, int PageSize, bool IncludeInactive, string? Term, CatalogStatus? Status) : IRequest<Result<PagedResult<PackageDto>>>;
