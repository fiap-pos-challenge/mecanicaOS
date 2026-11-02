using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Oficina.Infrastructure.Security;

namespace Oficina.FunctionalTests.Security;

public sealed class HttpCurrentUserTests
{
    [Fact]
    public void UserId_AuthenticatedRequest_ReturnsTheSubjectClaim()
    {
        var userId = Guid.CreateVersion7();

        var currentUser = new HttpCurrentUser(AccessorWith(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())));

        Assert.Equal(userId, currentUser.UserId);
    }

    [Fact]
    public void UserId_InvalidSubject_ReturnsNull()
    {
        Assert.Null(new HttpCurrentUser(AccessorWith(new Claim(JwtRegisteredClaimNames.Sub, "admin"))).UserId);
    }

    [Fact]
    public void UserId_AnonymousRequest_ReturnsNull()
    {
        Assert.Null(new HttpCurrentUser(AccessorWith()).UserId);
    }

    [Fact]
    public void UserId_OutsideARequest_ReturnsNull()
    {
        Assert.Null(new HttpCurrentUser(new HttpContextAccessor()).UserId);
    }

    private static HttpContextAccessor AccessorWith(params Claim[] claims) =>
        new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) } };
}
