using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderQuoteDto(
    decimal ServicesTotal,
    decimal PartsTotal,
    decimal Total,
    QuoteStatus Status,
    DateTimeOffset SentAt,
    DateTimeOffset? RespondedAt,
    string? RejectionReason);
