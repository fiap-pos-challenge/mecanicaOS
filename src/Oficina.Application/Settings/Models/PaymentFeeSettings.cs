namespace Oficina.Application.Settings.Models;

public sealed record PaymentFeeSettings(string Method, decimal FeePercent, int SettlementDays);
