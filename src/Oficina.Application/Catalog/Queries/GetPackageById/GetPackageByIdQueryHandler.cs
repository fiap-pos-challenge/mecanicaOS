using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.GetPackageById;

public sealed class GetPackageByIdQueryHandler(IPackageQueries queries)
    : IRequestHandler<GetPackageByIdQuery, Result<PackageDto>>
{
    public async Task<Result<PackageDto>> Handle(GetPackageByIdQuery query, CancellationToken cancellationToken)
    {
        return await queries.GetByIdAsync(query.Id, cancellationToken) is { } package ? package : PackageErrors.NotFound;
    }
}
