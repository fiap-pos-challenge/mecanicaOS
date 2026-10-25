using MediatR;
using Oficina.Application.Identity.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Identity.Commands.Login;

public sealed record LoginCommand(string Email, string Password, string? Device = null) : IRequest<Result<LoginResult>>;
