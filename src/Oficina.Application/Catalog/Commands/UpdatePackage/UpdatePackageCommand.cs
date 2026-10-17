using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.UpdatePackage;

public sealed record UpdatePackageCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    IReadOnlyList<PackageItemRequest> Items,
    int? MinMileage,
    int? MaxMileage) : IRequest<Result<PackageDto>>;
