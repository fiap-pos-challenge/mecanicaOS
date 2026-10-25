namespace Oficina.Application.Finance.Dtos;

public sealed record PaymentMethodTotalDto(string Method, long Amount, int Count);
