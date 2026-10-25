namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderTimeEntryDto(Guid Id, Guid MechanicId, string MechanicName, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, int Minutes);
