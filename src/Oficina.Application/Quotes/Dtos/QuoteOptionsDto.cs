namespace Oficina.Application.Quotes.Dtos;

public sealed record QuoteOptionsDto(
    IReadOnlyList<QuoteOptionDto> Packages,
    IReadOnlyList<QuoteOptionDto> Services,
    IReadOnlyList<QuoteOptionDto> Parts,
    IReadOnlyList<QuoteOptionDto> Suggestions,
    IReadOnlyList<DiscountRuleDto> DiscountRules,
    decimal DiscountLimitPercent,
    PaymentSettingsDto Payment,
    IReadOnlyList<int> ExpirationDays);
