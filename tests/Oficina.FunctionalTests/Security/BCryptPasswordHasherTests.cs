using Oficina.Infrastructure.Security;

namespace Oficina.FunctionalTests.Security;

public sealed class BCryptPasswordHasherTests
{
    private const string Password = "Oficina@2026";
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesASaltedBCryptHashWithWorkFactor12()
    {
        var first = _hasher.Hash(Password);
        var second = _hasher.Hash(Password);

        Assert.StartsWith("$2", first, StringComparison.Ordinal);
        Assert.Contains("$12$", first, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, first, StringComparison.Ordinal);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_RightPassword_ReturnsTrue()
    {
        Assert.True(_hasher.Verify(Password, _hasher.Hash(Password)));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("Outra@2026", _hasher.Hash(Password)));
    }

    [Fact]
    public void DummyHash_HasTheSameCostAndMatchesNoPassword()
    {
        Assert.Contains("$12$", _hasher.DummyHash, StringComparison.Ordinal);
        Assert.Same(_hasher.DummyHash, new BCryptPasswordHasher().DummyHash);
        Assert.False(_hasher.Verify(Password, _hasher.DummyHash));
        Assert.False(_hasher.Verify(string.Empty, _hasher.DummyHash));
    }
}
