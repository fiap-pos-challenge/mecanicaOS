namespace Oficina.Application.Automations.Queries;

public sealed record RuleStats(Guid RuleId, int Runs24h, DateTimeOffset? LastRunAt);
