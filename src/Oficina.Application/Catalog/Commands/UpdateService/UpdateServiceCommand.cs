using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.UpdateService;

public sealed record UpdateServiceCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal BasePrice,
    int EstimatedMinutes,
    decimal CommissionPercent,
    int WarrantyDays,
    IReadOnlyList<UsualPartInput> UsualParts) : IRequest<Result<ServiceDto>>;
