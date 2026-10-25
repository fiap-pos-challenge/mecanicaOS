using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.UnitTests.Domain.Common.ValueObjects;

public sealed class EmailTests
{
    [Theory]
    [InlineData("maria@oficina.com.br", "maria@oficina.com.br")]
    [InlineData("  Joao.Silva@Exemplo.COM ", "joao.silva@exemplo.com")]
    public void Create_WithValidAddress_NormalizesToTrimmedLowercase(string input, string expected)
    {
        var result = Email.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sem-arroba.com")]
    [InlineData("a@b")]
    [InlineData("a @b.com")]
    [InlineData("a@@b.com")]
    public void Create_WithInvalidFormat_ReturnsInvalidEmail(string input)
    {
        var result = Email.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrors.InvalidEmail, result.Error);
    }

    [Fact]
    public void Create_LongerThan160Characters_ReturnsInvalidEmail()
    {
        var input = new string('a', 150) + "@exemplo.com";

        var result = Email.Create(input);

        Assert.Equal(CommonErrors.InvalidEmail, result.Error);
    }

    [Fact]
    public void Equals_SameNormalizedAddress_AreEqual()
    {
        Assert.Equal(Email.Create("A@B.COM").Value, Email.Create("a@b.com").Value);
    }
}
