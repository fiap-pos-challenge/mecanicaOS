using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Users.Commands.ReactivateUser;

public sealed class ReactivateUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    : IRequestHandler<ReactivateUserCommand, Result>
{
    public async Task<Result> Handle(ReactivateUserCommand command, CancellationToken cancellationToken)
    {
        if (await users.GetByIdAsync(command.Id, cancellationToken) is not { } user)
        {
            return IdentityErrors.NotFound;
        }

        var reactivated = user.Reactivate();
        if (reactivated.IsFailure)
        {
            return reactivated;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
