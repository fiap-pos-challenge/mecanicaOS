namespace Oficina.Application.Analytics;

public sealed record PendingQuoteFact(Guid EstimateId, long Number, DateTimeOffset SentAt, string Channel);
