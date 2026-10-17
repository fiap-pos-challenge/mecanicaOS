using MediatR;
using Oficina.Application.Team.Dtos;
using Oficina.Application.Team.Roles;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Team.Queries.ListRoles;

public sealed class ListRolesQueryHandler : IRequestHandler<ListRolesQuery, Result<RoleListDto>>
{
    public Task<Result<RoleListDto>> Handle(ListRolesQuery query, CancellationToken cancellationToken) =>
        Task.FromResult<Result<RoleListDto>>(new RoleListDto(
            false,
            RolePermissions.Roles.Select(role => new RoleDefinitionDto(role.Role, role.Permissions, false)).ToList()));
}
