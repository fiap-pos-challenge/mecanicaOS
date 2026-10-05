using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Identity.Services;

namespace Oficina.Application.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    UserAdministrationPolicy policy,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteUserCommand, Result>
{
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actingUserId)
        {
            return CommonErrors.Unauthorized;
        }

        if (await users.GetByIdAsync(command.Id, cancellationToken) is not { } user)
        {
            return IdentityErrors.NotFound;
        }

        var allowed = await policy.EnsureCanDeleteAsync(actingUserId, user, cancellationToken);
        if (allowed.IsFailure)
        {
            return allowed;
        }

        user.Delete();
        users.Remove(user);
        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
