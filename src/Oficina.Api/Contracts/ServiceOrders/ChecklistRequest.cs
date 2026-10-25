using Oficina.Application.ServiceOrders.Dtos;

namespace Oficina.Api.Contracts.ServiceOrders;

public sealed record ChecklistRequest(IReadOnlyList<ChecklistChange> Items);
