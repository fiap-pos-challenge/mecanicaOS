namespace Oficina.Application.Finance.Dtos;

public sealed record ReceiptDto(Guid ServiceOrderId, long Number, string CustomerName, DateTimeOffset DeliveredAt, string Method, long Amount);
