using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderDetailDto(
    Guid Id,
    long Number,
    ServiceOrderStatus Status,
    string ProblemDescription,
    decimal TotalAmount,
    ServiceOrderCustomerDto Customer,
    ServiceOrderVehicleDto Vehicle,
    IReadOnlyList<ServiceOrderServiceItemDto> Services,
    IReadOnlyList<ServiceOrderPartItemDto> Parts,
    ServiceOrderQuoteDto? Quote,
    IReadOnlyList<ServiceOrderStatusHistoryDto> StatusHistory,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ExecutionStartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? DeliveredAt);
