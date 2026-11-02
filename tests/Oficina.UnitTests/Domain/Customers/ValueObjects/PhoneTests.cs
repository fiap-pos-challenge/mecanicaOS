using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Domain.Customers.ValueObjects;

public sealed class PhoneTests
{
    [Theory]
    [InlineData("11987654321", "11987654321")]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData("(21) 3456-7890", "2134567890")]
    public void Create_WithTenOrElevenDigits_StoresDigits(string input, string expected)
    {
        var result = Phone.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("987654321")]
    [InlineData("119876543210")]
    [InlineData("01987654321")]
    [InlineData("telefone")]
    public void Create_WithInvalidNumber_ReturnsInvalidPhone(string input)
    {
        Assert.Equal(CustomerErrors.InvalidPhone, Phone.Create(input).Error);
    }
}
