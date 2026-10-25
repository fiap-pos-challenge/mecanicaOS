using MediatR;
using Oficina.Application.Portal.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Commands.RejectQuoteFromPortal;

public sealed record RejectQuoteFromPortalCommand(string? Session, string? Reason) : IRequest<Result<PortalOrderDto>>;
