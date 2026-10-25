namespace Oficina.Application.Team.Dtos;

public sealed record RoleDefinitionDto(string Role, IReadOnlyList<string> Permissions, bool Editable);
