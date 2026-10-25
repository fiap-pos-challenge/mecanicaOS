using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderSummaryDto(
    Guid Id,
    long Number,
    ServiceOrderStatus Status,
    int Version,
    string LicensePlate,
    string CustomerName,
    string Vehicle,
    string Summary,
    string? MechanicName,
    DateTimeOffset? PromisedAt,
    bool HasQuoteItems,
    IReadOnlyList<string> Tags,
    DateTimeOffset OpenedAt,
    DateTimeOffset UpdatedAt,
    decimal TotalAmount);
