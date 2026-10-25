using MediatR;
using Oficina.Application.Identity.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Identity.Commands.Reauthenticate;

public sealed record ReauthenticateCommand(string? Password, string? Code) : IRequest<Result<ReauthenticationDto>>;
