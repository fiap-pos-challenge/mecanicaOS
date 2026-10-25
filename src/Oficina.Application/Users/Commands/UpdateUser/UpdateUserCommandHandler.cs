using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Users.Dtos;
using Oficina.Application.Users.Mappers;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Identity.Services;

namespace Oficina.Application.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    UserAdministrationPolicy policy,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actingUserId)
        {
            return CommonErrors.Unauthorized;
        }

        if (await users.GetByIdAsync(command.Id, cancellationToken) is not { } user)
        {
            return IdentityErrors.NotFound;
        }

        if ((await policy.EnsureCanChangeRoleAsync(actingUserId, user, command.Role, cancellationToken)).Error is { } blocked)
        {
            return blocked;
        }

        if (user.ChangeName(command.Name).Error is { } invalidName)
        {
            return invalidName;
        }

        user.ChangeRole(command.Role);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return user.ToDto();
    }
}
