using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Enums;

namespace Oficina.Application.Inventory.Commands.CreatePart;

public sealed record CreatePartCommand(
    string Code,
    string Name,
    PartType Type,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal MinimumStock) : IRequest<Result<PartDto>>;
