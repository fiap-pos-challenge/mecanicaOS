namespace Oficina.Application.Analytics;

public sealed record OpenOrderFact(Guid Id, long Number, string Status, string Vehicle, DateTimeOffset? PromisedAt, bool MissingPart, decimal Total);
