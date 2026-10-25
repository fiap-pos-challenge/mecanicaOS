namespace Oficina.Api.Contracts.Jobs;

public sealed record CustomerSummariesBody(IReadOnlyList<Guid>? CustomerIds);
