using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.UnitTests.Domain.ServiceOrders.ValueObjects;

public sealed class ServiceOrderValueObjectTests
{
    [Fact]
    public void ServiceOrderNumber_Positive_Succeeds()
    {
        Assert.Equal(15, ServiceOrderNumber.Create(15).Value.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ServiceOrderNumber_NotPositive_ReturnsInvalidNumber(long number)
    {
        Assert.Equal(ServiceOrderErrors.InvalidNumber, ServiceOrderNumber.Create(number).Error);
    }

    [Fact]
    public void ApprovalToken_WithSha256Hex_NormalizesToLowercase()
    {
        var hash = new string('A', 32) + new string('0', 32);

        var result = ApprovalToken.Create(hash);

        Assert.True(result.IsSuccess);
        Assert.Equal(hash.ToLowerInvariant(), result.Value.Hash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    public void ApprovalToken_WithInvalidHash_ReturnsInvalidApprovalToken(string hash)
    {
        Assert.Equal(ServiceOrderErrors.InvalidApprovalToken, ApprovalToken.Create(hash).Error);
    }

    [Fact]
    public void ApprovalTokenFromRawToken_ReturnsTheSha256HexOfTheToken()
    {
        var result = ApprovalToken.FromRawToken("abc");

        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", result.Value.Hash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ApprovalTokenFromRawToken_Blank_ReturnsInvalidApprovalToken(string rawToken)
    {
        Assert.Equal(ServiceOrderErrors.InvalidApprovalToken, ApprovalToken.FromRawToken(rawToken).Error);
    }
}
