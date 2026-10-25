namespace Oficina.Api.Contracts.Team;

public sealed record TeamProfilesQueryRequest(IReadOnlyList<Guid> UserIds);
