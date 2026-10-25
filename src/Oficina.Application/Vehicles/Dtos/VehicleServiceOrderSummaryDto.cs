using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Application.Vehicles.Dtos;

public sealed record VehicleServiceOrderSummaryDto(long Number, ServiceOrderStatus Status, DateTimeOffset OpenedAt, DateTimeOffset? CompletedAt);
