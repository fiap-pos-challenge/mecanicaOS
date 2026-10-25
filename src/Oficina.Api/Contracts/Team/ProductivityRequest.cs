namespace Oficina.Api.Contracts.Team;

public sealed record ProductivityRequest(IReadOnlyList<Guid> UserIds, int PeriodDays);
