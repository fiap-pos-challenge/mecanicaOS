namespace Oficina.Application.Team.Dtos;

public sealed record RoleListDto(bool Customizable, IReadOnlyList<RoleDefinitionDto> Items);
