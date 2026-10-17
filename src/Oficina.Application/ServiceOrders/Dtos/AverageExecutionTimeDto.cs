namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record AverageExecutionTimeDto(long? AverageMinutes, int TotalServiceOrders);
