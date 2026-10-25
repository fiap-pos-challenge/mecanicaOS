using MediatR;
using Oficina.Application.Portal.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Commands.CreatePixCharge;

public sealed record CreatePixChargeCommand(string? Session) : IRequest<Result<PixChargeDto>>;
