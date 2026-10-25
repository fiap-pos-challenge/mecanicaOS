namespace Oficina.Domain.ServiceOrders.Entities;

public sealed record ChecklistItem(string Id, string Label, string? State)
{
    public static readonly IReadOnlyList<string> States = ["Ok", "Attention", "Replace"];

    public static readonly IReadOnlyList<string> DefaultLabels = ["Óleo e filtros", "Freios", "Suspensão dianteira", "Pneus", "Luzes", "Bateria"];
}
