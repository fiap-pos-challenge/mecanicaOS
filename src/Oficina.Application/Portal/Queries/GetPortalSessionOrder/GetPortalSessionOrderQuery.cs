using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Queries.GetPortalSessionOrder;

public sealed record GetPortalSessionOrderQuery(string? Session) : IRequest<Result<Guid>>;
