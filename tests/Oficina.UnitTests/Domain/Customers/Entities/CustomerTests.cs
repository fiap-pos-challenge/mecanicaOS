using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Enums;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Domain.Customers.Entities;

public sealed class CustomerTests
{
    private static readonly Document Cpf = Document.Create("52998224725").Value;
    private static readonly Phone MobilePhone = Phone.Create("11987654321").Value;
    private static readonly Email ContactEmail = Email.Create("maria@exemplo.com").Value;

    [Fact]
    public void Create_WithValidData_SetsFieldsAndGeneratesId()
    {
        var address = Address.CreateOptional("01310100", "Avenida Paulista", "1000", null, "Bela Vista", "São Paulo", "SP").Value;

        var result = Customer.Create("  Maria Souza ", Cpf, ContactEmail, MobilePhone, address);

        Assert.True(result.IsSuccess);
        var customer = result.Value;
        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("Maria Souza", customer.Name);
        Assert.Equal(Cpf, customer.Document);
        Assert.Equal(DocumentType.Cpf, customer.DocumentType);
        Assert.Equal(ContactEmail, customer.Email);
        Assert.Equal(MobilePhone, customer.Phone);
        Assert.Equal(address, customer.Address);
    }

    [Fact]
    public void Create_WithoutEmailAndAddress_Succeeds()
    {
        var result = Customer.Create("Maria Souza", Cpf, null, MobilePhone, null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Email);
        Assert.Null(result.Value.Address);
    }

    [Fact]
    public void Create_WithCnpj_ExposesCnpjType()
    {
        var cnpj = Document.Create("11222333000181").Value;

        var customer = Customer.Create("Auto Peças Ltda", cnpj, null, MobilePhone, null).Value;

        Assert.Equal(DocumentType.Cnpj, customer.DocumentType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Create_WithTooShortName_ReturnsInvalidName(string name)
    {
        Assert.Equal(CustomerErrors.InvalidName, Customer.Create(name, Cpf, null, MobilePhone, null).Error);
    }

    [Fact]
    public void Create_WithNameAbove160Characters_ReturnsInvalidName()
    {
        Assert.Equal(CustomerErrors.InvalidName, Customer.Create(new string('M', 161), Cpf, null, MobilePhone, null).Error);
    }

    [Fact]
    public void ChangeContact_WithValidData_UpdatesNameEmailAndPhone()
    {
        var customer = Customer.Create("Maria Souza", Cpf, ContactEmail, MobilePhone, null).Value;
        var newPhone = Phone.Create("2134567890").Value;

        var result = customer.ChangeContact("Maria S. Lima", null, newPhone);

        Assert.True(result.IsSuccess);
        Assert.Equal("Maria S. Lima", customer.Name);
        Assert.Null(customer.Email);
        Assert.Equal(newPhone, customer.Phone);
        Assert.Equal(Cpf, customer.Document);
    }

    [Fact]
    public void ChangeContact_WithInvalidName_KeepsPreviousData()
    {
        var customer = Customer.Create("Maria Souza", Cpf, ContactEmail, MobilePhone, null).Value;

        var result = customer.ChangeContact("", null, Phone.Create("2134567890").Value);

        Assert.Equal(CustomerErrors.InvalidName, result.Error);
        Assert.Equal("Maria Souza", customer.Name);
        Assert.Equal(ContactEmail, customer.Email);
        Assert.Equal(MobilePhone, customer.Phone);
    }

    [Fact]
    public void ChangeAddress_ReplacesOrRemovesAddress()
    {
        var customer = Customer.Create("Maria Souza", Cpf, null, MobilePhone, null).Value;
        var address = Address.CreateOptional("13010000", "Rua A", "10", null, "Centro", "Campinas", "SP").Value;

        customer.ChangeAddress(address);
        Assert.Equal(address, customer.Address);

        customer.ChangeAddress(null);
        Assert.Null(customer.Address);
    }
}
