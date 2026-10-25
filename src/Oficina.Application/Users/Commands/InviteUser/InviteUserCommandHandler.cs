using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Users.Dtos;
using Oficina.Application.Users.Mappers;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Users.Commands.InviteUser;

public sealed class InviteUserCommandHandler(
    IUserRepository users,
    InvitationIssuer issuer,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<InviteUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(InviteUserCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        if (email.Error is { } invalidEmail)
        {
            return invalidEmail;
        }

        if (await users.GetByEmailAsync(email.Value, cancellationToken) is not null)
        {
            return IdentityErrors.DuplicateEmail;
        }

        var user = User.Invite(command.Name, email.Value, command.Role);
        if (user.Error is { } invalidUser)
        {
            return invalidUser;
        }

        users.Add(user.Value);
        var issued = await issuer.IssueAsync(user.Value, currentUser.UserId, cancellationToken);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        await issuer.SendAsync(user.Value, issued, cancellationToken);
        return user.Value.ToDto(issued.Invitation.ExpiresAt);
    }
}
