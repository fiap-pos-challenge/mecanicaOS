using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderSummaryDto(
    Guid Id,
    long Number,
    ServiceOrderStatus Status,
    string LicensePlate,
    string CustomerName,
    DateTimeOffset OpenedAt,
    decimal TotalAmount);
