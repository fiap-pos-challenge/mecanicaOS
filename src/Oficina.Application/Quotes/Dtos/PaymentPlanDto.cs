namespace Oficina.Application.Quotes.Dtos;

public sealed record PaymentPlanDto(IReadOnlyList<string> Methods, int MaxInstallments);
