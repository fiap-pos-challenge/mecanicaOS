using Oficina.Domain.Customers.Enums;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Domain.Customers.ValueObjects;

public sealed class DocumentTests
{
    [Theory]
    [InlineData("52998224725", "52998224725")]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("111.444.777-35", "11144477735")]
    public void Create_WithValidCpf_StoresDigitsAndCpfType(string input, string expected)
    {
        var result = Document.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
        Assert.Equal(DocumentType.Cpf, result.Value.Type);
    }

    [Theory]
    [InlineData("11222333000181", "11222333000181")]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData("04.252.011/0001-10", "04252011000110")]
    public void Create_WithValidCnpj_StoresDigitsAndCnpjType(string input, string expected)
    {
        var result = Document.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
        Assert.Equal(DocumentType.Cnpj, result.Value.Type);
    }

    [Theory]
    [InlineData("52998224724")]
    [InlineData("529.982.247-35")]
    [InlineData("11222333000182")]
    [InlineData("11222333000191")]
    public void Create_WithWrongCheckDigit_ReturnsInvalidDocument(string input)
    {
        Assert.Equal(CustomerErrors.InvalidDocument, Document.Create(input).Error);
    }

    [Theory]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    [InlineData("99999999999999")]
    public void Create_WithRepeatedDigits_ReturnsInvalidDocument(string input)
    {
        Assert.Equal(CustomerErrors.InvalidDocument, Document.Create(input).Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("5299822472")]
    [InlineData("529982247250")]
    [InlineData("1122233300018")]
    [InlineData("abc.def.ghi-jk")]
    public void Create_WithWrongLength_ReturnsInvalidDocument(string input)
    {
        Assert.Equal(CustomerErrors.InvalidDocument, Document.Create(input).Error);
    }
}
