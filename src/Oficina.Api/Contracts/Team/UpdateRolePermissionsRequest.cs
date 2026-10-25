namespace Oficina.Api.Contracts.Team;

public sealed record UpdateRolePermissionsRequest(IReadOnlyList<string> Permissions);
