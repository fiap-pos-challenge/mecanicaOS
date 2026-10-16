using Oficina.Application.Identity.Invitations;
using Oficina.Domain.Identity.Entities;

namespace Oficina.UnitTests.Application.Identity;

public sealed class InvitationTokenTests
{
    [Fact]
    public void Generate_ReturnsUrlSafeRandomTokens()
    {
        var first = InvitationToken.Generate();
        var second = InvitationToken.Generate();

        Assert.NotEqual(first, second);
        Assert.Equal(43, first.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", first);
    }

    [Fact]
    public void Hash_IsStableLowercaseSha256()
    {
        var hash = InvitationToken.Hash("abc");

        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", hash);
        Assert.Equal(UserInvitation.TokenHashLength, hash.Length);
    }
}
