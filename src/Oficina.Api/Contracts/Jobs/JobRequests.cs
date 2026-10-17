namespace Oficina.Api.Contracts.Jobs;

public sealed record CustomerSummariesBody(IReadOnlyList<Guid>? CustomerIds);

public sealed record ImportCustomersBody(string FileName, string Csv);
