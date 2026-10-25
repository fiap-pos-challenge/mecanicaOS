using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Dtos;
using Oficina.Application.Identity.Invitations;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Identity.Sessions;

public sealed class SessionIssuer(IUserSessionRepository sessions, ITokenGenerator tokenGenerator)
{
    public LoginResult Start(User user, string? device, DateTimeOffset now)
    {
        var refreshToken = InvitationToken.Generate();
        var session = UserSession.Start(user.Id, InvitationToken.Hash(refreshToken), device, now);
        sessions.Add(session);
        return Issue(user, session, refreshToken);
    }

    public LoginResult Rotate(User user, UserSession session, DateTimeOffset now)
    {
        var refreshToken = InvitationToken.Generate();
        session.Rotate(InvitationToken.Hash(refreshToken), now);
        return Issue(user, session, refreshToken);
    }

    private LoginResult Issue(User user, UserSession session, string refreshToken)
    {
        var token = tokenGenerator.Generate(user, session.Id);
        return new LoginResult(new LoginResponse(token.Value, token.ExpiresAt, false, null), refreshToken, session.ExpiresAt);
    }
}
