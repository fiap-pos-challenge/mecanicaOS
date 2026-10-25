namespace Oficina.Application.Finance.Dtos;

public sealed record ReceivableDto(Guid ServiceOrderId, long Number, string CustomerName, DateTimeOffset? CompletedAt, long Amount, int DaysWaiting);
