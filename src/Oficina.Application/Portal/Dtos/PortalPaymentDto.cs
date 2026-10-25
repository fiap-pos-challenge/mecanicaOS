namespace Oficina.Application.Portal.Dtos;

public sealed record PortalPaymentDto(string Status, decimal AmountDue, IReadOnlyList<string> Methods);
