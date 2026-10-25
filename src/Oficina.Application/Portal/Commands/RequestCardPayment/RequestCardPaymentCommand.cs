using MediatR;
using Oficina.Application.Portal.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Portal.Commands.RequestCardPayment;

public sealed record RequestCardPaymentCommand(string? Session) : IRequest<Result<CardRedirectDto>>;
