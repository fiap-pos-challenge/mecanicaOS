namespace Oficina.Application.Finance;

public sealed record ReceivableFact(Guid ServiceOrderId, long Number, string CustomerName, DateTimeOffset? CompletedAt, decimal Amount);
