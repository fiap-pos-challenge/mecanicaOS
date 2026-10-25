namespace Oficina.Application.Quotes.Dtos;

public sealed record PaymentFeeDto(string Method, decimal FeePercent, int SettlementDays);
