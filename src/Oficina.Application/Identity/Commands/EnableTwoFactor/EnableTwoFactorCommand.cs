using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Identity.Commands.EnableTwoFactor;

public sealed record EnableTwoFactorCommand(string Code) : IRequest<Result>;
