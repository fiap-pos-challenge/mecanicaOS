using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Enums;

namespace Oficina.Application.Catalog.Commands.UpdatePart;

public sealed record UpdatePartCommand(
    Guid Id,
    string Code,
    string Name,
    PartType Type,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal Cost,
    decimal MinimumStock,
    string? Supplier,
    string? Barcode) : IRequest<Result<PartDto>>;
