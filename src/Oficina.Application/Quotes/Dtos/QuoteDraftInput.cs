namespace Oficina.Application.Quotes.Dtos;

public sealed record QuoteDraftInput(IReadOnlyList<QuoteDraftItemInput> Items, string DiscountMode, ManualDiscountDto? ManualDiscount, PaymentPlanDto? Payment);
