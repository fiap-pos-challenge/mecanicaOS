namespace Oficina.Application.Automations.Queries;

public sealed record AutomationContext(
    Guid? ServiceOrderId,
    long? Number,
    string? CustomerName,
    string? Vehicle,
    string? Email,
    string? Phone,
    bool ConsentEmail,
    bool ConsentWhatsApp,
    decimal? Amount,
    string? OrderStatus,
    string? QuoteStatus,
    int? DaysOverdue,
    decimal? PartStock,
    Guid? PartId,
    string? PartName,
    string? Link);
