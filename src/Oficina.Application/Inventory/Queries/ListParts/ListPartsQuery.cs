using MediatR;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.ListParts;

public sealed record ListPartsQuery(int Page, int PageSize, bool IncludeInactive, bool BelowMinimumOnly)
    : IRequest<Result<PagedResult<PartDto>>>;
