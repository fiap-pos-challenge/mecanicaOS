using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.UpdateService;

public sealed record UpdateServiceCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal BasePrice,
    int EstimatedMinutes) : IRequest<Result<ServiceDto>>;
