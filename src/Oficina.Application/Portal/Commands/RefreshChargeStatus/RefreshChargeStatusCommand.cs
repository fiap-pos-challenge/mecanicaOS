using MediatR;
using Oficina.Application.Portal.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Commands.RefreshChargeStatus;

public sealed record RefreshChargeStatusCommand(string? Session, Guid PaymentId) : IRequest<Result<ChargeStatusDto>>;
