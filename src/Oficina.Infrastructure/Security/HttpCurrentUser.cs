using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Oficina.Application.Abstractions.Security;

namespace Oficina.Infrastructure.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)
            ? userId
            : null;

    public Guid? SessionId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(JwtTokenGenerator.SessionClaimType)?.Value, out var sessionId)
            ? sessionId
            : null;
}
