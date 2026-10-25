using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Users.Dtos;
using Oficina.Application.Users.Mappers;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Users.Commands.ResendInvitation;

public sealed class ResendInvitationCommandHandler(
    IUserRepository users,
    InvitationIssuer issuer,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<ResendInvitationCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(ResendInvitationCommand command, CancellationToken cancellationToken)
    {
        if (await users.GetByIdAsync(command.Id, cancellationToken) is not { } user)
        {
            return IdentityErrors.NotFound;
        }

        if (!user.HasPendingInvitation)
        {
            return IdentityErrors.InvitationNotPending;
        }

        var issued = await issuer.IssueAsync(user, currentUser.UserId, cancellationToken);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        await issuer.SendAsync(user, issued, cancellationToken);
        return user.ToDto(issued.Invitation.ExpiresAt);
    }
}
