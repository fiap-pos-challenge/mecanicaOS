using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderBoardQuoteDto(
    Guid Id,
    long Number,
    int Version,
    QuoteStatus Status,
    decimal Total,
    decimal ServicesTotal,
    decimal PartsTotal,
    DateTimeOffset SentAt,
    DateTimeOffset? RespondedAt,
    string? RejectionReason,
    IReadOnlyList<ServiceOrderBoardQuoteItemDto> Items);
