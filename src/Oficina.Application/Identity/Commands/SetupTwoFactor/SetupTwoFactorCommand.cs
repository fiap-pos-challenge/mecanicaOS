using MediatR;
using Oficina.Application.Identity.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Identity.Commands.SetupTwoFactor;

public sealed record SetupTwoFactorCommand : IRequest<Result<TwoFactorSetupDto>>;
