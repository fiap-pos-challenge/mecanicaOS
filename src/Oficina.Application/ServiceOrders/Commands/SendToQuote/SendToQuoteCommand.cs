using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.SendToQuote;

public sealed record SendToQuoteCommand(Guid Id, string? Description, IReadOnlyList<QuoteRequestItem> Items) : IRequest<Result<MechanicOrderDto>>;
