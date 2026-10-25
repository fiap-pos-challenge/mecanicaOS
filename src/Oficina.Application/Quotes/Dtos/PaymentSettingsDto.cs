namespace Oficina.Application.Quotes.Dtos;

public sealed record PaymentSettingsDto(IReadOnlyList<string> Methods, int MaxInstallments, int InterestFreeInstallments, decimal MonthlyInterestPercent, IReadOnlyList<PaymentFeeDto> Fees);
