namespace Oficina.Application.Team.Dtos;

public sealed record ProductivityDto(
    Guid UserId,
    int ServiceOrders,
    decimal Hours,
    decimal OccupancyPercent,
    decimal Revenue,
    decimal ReworkPercent,
    IReadOnlyList<int> Weekly);
