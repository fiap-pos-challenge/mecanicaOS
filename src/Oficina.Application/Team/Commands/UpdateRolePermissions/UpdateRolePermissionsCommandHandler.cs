using MediatR;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Team.Errors;

namespace Oficina.Application.Team.Commands.UpdateRolePermissions;

public sealed class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, Result>
{
    public Task<Result> Handle(UpdateRolePermissionsCommand command, CancellationToken cancellationToken) =>
        Task.FromResult<Result>(TeamErrors.RolesNotCustomizable);
}
