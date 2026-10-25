using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Identity.Entities;

namespace Oficina.Infrastructure.Security;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenGenerator
{
    public const string RoleClaimType = "role";
    public const string SessionClaimType = "sid";

    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken Generate(User user, Guid? sessionId = null)
    {
        var settings = options.Value;
        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(settings.ExpirationMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret ?? string.Empty));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(RoleClaimType, user.Role.ToString()),
        };
        if (sessionId is { } session)
        {
            claims.Add(new Claim(SessionClaimType, session.ToString()));
        }

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }
}
