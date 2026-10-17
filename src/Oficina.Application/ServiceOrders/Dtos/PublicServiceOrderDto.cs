using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record PublicServiceOrderDto(
    long Number,
    ServiceOrderStatus Status,
    string ProblemDescription,
    decimal TotalAmount,
    ServiceOrderVehicleDto Vehicle,
    IReadOnlyList<ServiceOrderServiceItemDto> Services,
    IReadOnlyList<ServiceOrderPartItemDto> Parts,
    ServiceOrderQuoteDto? Quote,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ExecutionStartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? DeliveredAt);
