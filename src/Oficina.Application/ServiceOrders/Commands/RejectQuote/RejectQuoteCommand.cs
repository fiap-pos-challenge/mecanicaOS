using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.RejectQuote;

public sealed record RejectQuoteCommand(Guid ServiceOrderId, string Reason) : IRequest<Result<ServiceOrderDetailDto>>;
