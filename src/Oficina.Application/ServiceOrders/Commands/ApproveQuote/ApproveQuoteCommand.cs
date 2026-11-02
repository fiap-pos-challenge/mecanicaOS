using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.ApproveQuote;

public sealed record ApproveQuoteCommand(Guid ServiceOrderId) : IRequest<Result<ServiceOrderDetailDto>>;
