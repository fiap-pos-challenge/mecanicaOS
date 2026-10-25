using MediatR;
using Oficina.Application.Portal.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Commands.ConfirmPortalIdentity;

public sealed record ConfirmPortalIdentityCommand(string? Token, string? LastDigits) : IRequest<Result<PortalConfirmDto>>;
