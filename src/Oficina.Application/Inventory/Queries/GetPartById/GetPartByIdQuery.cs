using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.GetPartById;

public sealed record GetPartByIdQuery(Guid Id) : IRequest<Result<PartDetailDto>>;
