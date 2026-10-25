using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Team.Commands.UpdateRolePermissions;

public sealed record UpdateRolePermissionsCommand(string Role, IReadOnlyList<string> Permissions) : IRequest<Result>;
