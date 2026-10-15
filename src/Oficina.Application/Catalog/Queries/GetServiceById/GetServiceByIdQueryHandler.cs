using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.GetServiceById;

public sealed class GetServiceByIdQueryHandler(IServiceQueries queries) : IRequestHandler<GetServiceByIdQuery, Result<ServiceDto>>
{
    public async Task<Result<ServiceDto>> Handle(GetServiceByIdQuery query, CancellationToken cancellationToken)
    {
        if (await queries.GetByIdAsync(query.Id, cancellationToken) is not { } service)
        {
            return CatalogErrors.NotFound;
        }

        return service;
    }
}
