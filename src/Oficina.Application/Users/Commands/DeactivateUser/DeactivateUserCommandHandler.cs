using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Identity.Services;

namespace Oficina.Application.Users.Commands.DeactivateUser;

public sealed class DeactivateUserCommandHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    UserAdministrationPolicy policy,
    IUnitOfWork unitOfWork) : IRequestHandler<DeactivateUserCommand, Result>
{
    public async Task<Result> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actingUserId)
        {
            return CommonErrors.Unauthorized;
        }

        if (await users.GetByIdAsync(command.Id, cancellationToken) is not { } user)
        {
            return IdentityErrors.NotFound;
        }

        var allowed = await policy.EnsureCanDeactivateAsync(actingUserId, user, cancellationToken);
        if (allowed.IsFailure)
        {
            return allowed;
        }

        var deactivated = user.Deactivate();
        if (deactivated.IsFailure)
        {
            return deactivated;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
