using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.ListParts;

public sealed record ListPartsQuery(int Page, int PageSize, bool IncludeInactive, string? Term, CatalogStatus? Status, bool IncludeCost) : IRequest<Result<PagedResult<PartDto>>>;
