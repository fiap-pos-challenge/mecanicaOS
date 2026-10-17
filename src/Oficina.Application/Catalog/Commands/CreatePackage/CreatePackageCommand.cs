using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Commands.CreatePackage;

public sealed record CreatePackageCommand(
    string Name,
    string? Description,
    decimal Price,
    IReadOnlyList<PackageItemRequest> Items,
    int? MinMileage,
    int? MaxMileage) : IRequest<Result<PackageDto>>;
