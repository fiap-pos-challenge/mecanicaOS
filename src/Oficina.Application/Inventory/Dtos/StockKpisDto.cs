namespace Oficina.Application.Inventory.Dtos;

public sealed record StockKpisDto(
    int BelowMinimum,
    decimal SuggestedOrderTotal,
    decimal OutflowMonth,
    decimal? OutflowMonthDeltaPercent,
    decimal? TurnoverDays,
    int TurnoverGoalDays,
    int IdleItems,
    decimal IdleValue,
    decimal StockValue,
    int TotalItems);
