namespace Oficina.Application.Settings.Models;

public sealed record PaymentSettings(
    IReadOnlyList<string> Methods,
    int MaxInstallments,
    int InterestFreeInstallments,
    decimal MonthlyInterestPercent,
    IReadOnlyList<PaymentFeeSettings> Fees,
    string? PixKey);
