namespace Oficina.Application.Dashboard.Dtos;

public sealed record DashboardKpiDto(string Id, long Value, long? Previous, long? Detail);
