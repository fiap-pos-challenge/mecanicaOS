namespace Oficina.Application.Settings.Dtos;

public sealed record PaymentInput(IReadOnlyList<string> Methods, int MaxInstallments, int InterestFreeInstallments, decimal MonthlyInterestPercent, IReadOnlyList<Models.PaymentFeeSettings> Fees, string? PixKey);
