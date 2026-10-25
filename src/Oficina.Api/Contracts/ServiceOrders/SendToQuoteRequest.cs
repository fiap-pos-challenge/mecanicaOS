using Oficina.Application.ServiceOrders.Dtos;

namespace Oficina.Api.Contracts.ServiceOrders;

public sealed record SendToQuoteRequest(string? Description, IReadOnlyList<QuoteRequestItem>? Items);
