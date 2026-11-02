using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.RejectQuoteByToken;

public sealed record RejectQuoteByTokenCommand(string Token, string Reason) : IRequest<Result<PublicServiceOrderDto>>;
