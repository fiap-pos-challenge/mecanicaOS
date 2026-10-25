using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.GetPackageById;

public sealed record GetPackageByIdQuery(Guid Id) : IRequest<Result<PackageDto>>;
