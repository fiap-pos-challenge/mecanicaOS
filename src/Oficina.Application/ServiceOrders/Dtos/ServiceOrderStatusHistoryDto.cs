using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderStatusHistoryDto(
    ServiceOrderStatus? PreviousStatus,
    ServiceOrderStatus NewStatus,
    Guid? ChangedByUserId,
    string? ChangedBy,
    string? Note,
    DateTimeOffset ChangedAt);
