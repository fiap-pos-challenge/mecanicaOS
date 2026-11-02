using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.ValueObjects;

namespace Oficina.UnitTests.Domain.Inventory.ValueObjects;

public sealed class PartCodeTests
{
    [Theory]
    [InlineData("FLT-001", "FLT-001")]
    [InlineData(" oleo.5w30 ", "OLEO.5W30")]
    [InlineData("A", "A")]
    public void Create_WithValidCode_StoresTrimmedUppercase(string input, string expected)
    {
        var result = PartCode.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("FLT 001")]
    [InlineData("FLT_001")]
    [InlineData("FLT#1")]
    public void Create_WithInvalidCharactersOrEmpty_ReturnsInvalidPartCode(string input)
    {
        Assert.Equal(InventoryErrors.InvalidPartCode, PartCode.Create(input).Error);
    }

    [Fact]
    public void Create_Above30Characters_ReturnsInvalidPartCode()
    {
        Assert.Equal(InventoryErrors.InvalidPartCode, PartCode.Create(new string('A', 31)).Error);
    }
}
