using Oficina.Application.Automations.Queries;

namespace Oficina.Application.Automations;

public sealed record AutomationContextValues(string? OrderStatus, string? QuoteStatus, bool HasConsent, decimal? Amount, decimal? DaysOverdue, decimal? PartStock, string? CustomerName, long? Number, string? Vehicle, string? Link)
{
    public static AutomationContextValues From(AutomationContext context) =>
        new(context.OrderStatus, context.QuoteStatus, context.ConsentEmail || context.ConsentWhatsApp, context.Amount, context.DaysOverdue, context.PartStock, context.CustomerName, context.Number, context.Vehicle, context.Link);
}
