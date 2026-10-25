using MediatR;
using Oficina.Application.Identity.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Identity.Commands.RefreshSession;

public sealed record RefreshSessionCommand(string? RefreshToken) : IRequest<Result<LoginResult>>;
