using MediatR;
using Oficina.Application.Portal.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Queries.GetPortalAccess;

public sealed record GetPortalAccessQuery(string? Token) : IRequest<Result<PortalAccessDto>>;
