using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.GetPartById;

public sealed record GetPartByIdQuery(Guid Id, bool IncludeCost) : IRequest<Result<PartDto>>;
