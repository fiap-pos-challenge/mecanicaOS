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
}
