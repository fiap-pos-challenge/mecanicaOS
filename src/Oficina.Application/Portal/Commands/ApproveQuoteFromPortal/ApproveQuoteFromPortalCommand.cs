using MediatR;
using Oficina.Application.Portal.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Commands.ApproveQuoteFromPortal;

public sealed record ApproveQuoteFromPortalCommand(string? Session, IReadOnlyCollection<Guid> ItemIds) : IRequest<Result<PortalOrderDto>>;
