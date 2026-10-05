using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Domain.Customers.ValueObjects;

public sealed class AddressTests
{
    [Fact]
    public void CreateOptional_WithAllRequiredFields_CreatesAddress()
    {
        var result = Address.CreateOptional("01310-100", "Avenida Paulista", "1000", "Sala 10", "Bela Vista", "São Paulo", "sp");

        Assert.True(result.IsSuccess);
        var address = Assert.IsType<Address>(result.Value);
        Assert.Equal("01310100", address.ZipCode);
        Assert.Equal("Avenida Paulista", address.Street);
        Assert.Equal("1000", address.Number);
        Assert.Equal("Sala 10", address.Complement);
        Assert.Equal("Bela Vista", address.District);
        Assert.Equal("São Paulo", address.City);
        Assert.Equal("SP", address.State);
    }

    [Fact]
    public void CreateOptional_WithoutComplement_CreatesAddress()
    {
        var result = Address.CreateOptional("01310100", "Avenida Paulista", "S/N", null, "Bela Vista", "São Paulo", "SP");

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.IsType<Address>(result.Value).Complement);
    }

    [Fact]
    public void CreateOptional_WithNothingInformed_ReturnsNoAddress()
    {
        var result = Address.CreateOptional(null, null, null, null, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void CreateOptional_WithOnlyBlankValues_ReturnsNoAddress()
    {
        var result = Address.CreateOptional(" ", "", " ", " ", "", " ", "");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void CreateOptional_WithOnlyComplement_ReturnsIncompleteAddress()
    {
        var result = Address.CreateOptional(null, null, null, "Apto 1", null, null, null);

        Assert.Equal(CustomerErrors.IncompleteAddress, result.Error);
    }

    [Theory]
    [InlineData(null, "Rua A", "10", "Centro", "Campinas", "SP")]
    [InlineData("13010000", null, "10", "Centro", "Campinas", "SP")]
    [InlineData("13010000", "Rua A", null, "Centro", "Campinas", "SP")]
    [InlineData("13010000", "Rua A", "10", null, "Campinas", "SP")]
    [InlineData("13010000", "Rua A", "10", "Centro", null, "SP")]
    [InlineData("13010000", "Rua A", "10", "Centro", "Campinas", null)]
    public void CreateOptional_WithAnyRequiredFieldMissing_ReturnsIncompleteAddress(
        string? zipCode, string? street, string? number, string? district, string? city, string? state)
    {
        var result = Address.CreateOptional(zipCode, street, number, null, district, city, state);

        Assert.Equal(CustomerErrors.IncompleteAddress, result.Error);
    }

    [Theory]
    [InlineData("1301000")]
    [InlineData("130100000")]
    [InlineData("ABCDEFGH")]
    public void CreateOptional_WithInvalidZipCode_ReturnsInvalidZipCode(string zipCode)
    {
        var result = Address.CreateOptional(zipCode, "Rua A", "10", null, "Centro", "Campinas", "SP");

        Assert.Equal(CustomerErrors.InvalidZipCode, result.Error);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("S")]
    [InlineData("SPP")]
    public void CreateOptional_WithUnknownState_ReturnsInvalidState(string state)
    {
        var result = Address.CreateOptional("13010000", "Rua A", "10", null, "Centro", "Campinas", state);

        Assert.Equal(CustomerErrors.InvalidState, result.Error);
    }

    [Fact]
    public void CreateOptional_WithFieldAboveMaximumLength_ReturnsInvalidAddress()
    {
        var result = Address.CreateOptional("13010000", new string('R', 151), "10", null, "Centro", "Campinas", "SP");

        Assert.Equal(CustomerErrors.InvalidAddress, result.Error);
    }

    [Fact]
    public void CreateOptional_WithComplementAboveMaximumLength_ReturnsInvalidAddress()
    {
        var result = Address.CreateOptional("13010000", "Rua A", "10", new string('C', 61), "Centro", "Campinas", "SP");

        Assert.Equal(CustomerErrors.InvalidAddress, result.Error);
    }

    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        var first = Address.CreateOptional("13010000", "Rua A", "10", null, "Centro", "Campinas", "SP").Value;
        var second = Address.CreateOptional("13010-000", "Rua A", "10", " ", "Centro", "Campinas", "sp").Value;

        Assert.Equal(first, second);
    }
}
