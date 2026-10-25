using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Invitations;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Commands.AcceptInvitation;

public sealed class AcceptInvitationCommandHandler(
    IUserInvitationRepository invitations,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<AcceptInvitationCommand, Result>
{
    public async Task<Result> Handle(AcceptInvitationCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (await invitations.GetByTokenHashAsync(InvitationToken.Hash(command.Token), cancellationToken) is not { } invitation)
        {
            return IdentityErrors.InvitationNotFound;
        }

        if (await users.GetByIdAsync(invitation.UserId, cancellationToken) is not { } user)
        {
            return IdentityErrors.InvitationNotFound;
        }

        var accepted = invitation.Accept(now);
        if (accepted.IsFailure)
        {
            return accepted;
        }

        var defined = user.DefinePassword(passwordHasher.Hash(command.Password));
        if (defined.IsFailure)
        {
            return defined;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
