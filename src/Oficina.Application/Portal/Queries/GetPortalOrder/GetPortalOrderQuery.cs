using MediatR;
using Oficina.Application.Portal.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Queries.GetPortalOrder;

public sealed record GetPortalOrderQuery(string? Session) : IRequest<Result<PortalOrderDto>>;
