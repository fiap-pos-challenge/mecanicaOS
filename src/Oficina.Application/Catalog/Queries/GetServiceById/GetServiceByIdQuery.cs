using MediatR;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Catalog.Queries.GetServiceById;

public sealed record GetServiceByIdQuery(Guid Id) : IRequest<Result<ServiceDto>>;
