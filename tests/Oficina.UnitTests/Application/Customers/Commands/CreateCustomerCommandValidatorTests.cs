using Oficina.Application.Customers.Commands.CreateCustomer;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Customers.Errors;

namespace Oficina.UnitTests.Application.Customers.Commands;

public sealed class CreateCustomerCommandValidatorTests
{
    private readonly CreateCustomerCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        Assert.True(_validator.Validate(Command()).IsValid);
    }

    [Fact]
    public void Validate_CnpjWithoutOptionalFields_HasNoErrors()
    {
        Assert.True(_validator.Validate(Command() with { Document = CustomerTestData.Cnpj, Email = null, Address = null }).IsValid);
    }

    [Theory]
    [InlineData("", "Name", "customer.invalidName")]
    [InlineData("A", "Name", "customer.invalidName")]
    [InlineData("ok", "Document", "customer.invalidDocument")]
    [InlineData("ok", "Email", "common.invalidEmail")]
    [InlineData("ok", "Phone", "customer.invalidPhone")]
    public void Validate_InvalidField_ReturnsTheCatalogCode(string name, string field, string code)
    {
        var command = field switch
        {
            "Document" => Command() with { Document = "12345678900" },
            "Email" => Command() with { Email = "invalid" },
            "Phone" => Command() with { Phone = "0000" },
            _ => Command() with { Name = name },
        };

        var failure = Assert.Single(_validator.Validate(command).Errors);

        Assert.Equal(field, failure.PropertyName);
        Assert.Equal(code, failure.ErrorCode);
    }

    [Fact]
    public void Validate_EmptyDocumentAndPhone_ReturnsOneErrorEach()
    {
        var errors = _validator.Validate(Command() with { Document = string.Empty, Phone = string.Empty }).Errors;

        Assert.Equal(new[] { CustomerErrors.InvalidDocument.Code, CustomerErrors.InvalidPhone.Code }, errors.Select(error => error.ErrorCode));
    }

    [Fact]
    public void Validate_PartialAddress_ReturnsIncompleteAddress()
    {
        var failure = Assert.Single(_validator.Validate(Command() with { Address = CustomerTestData.AddressInput() with { Street = null } }).Errors);

        Assert.Equal("Address", failure.PropertyName);
        Assert.Equal(CustomerErrors.IncompleteAddress.Code, failure.ErrorCode);
        Assert.Equal(CustomerErrors.IncompleteAddress.Message, failure.ErrorMessage);
    }

    [Fact]
    public void Validate_Messages_ComeFromTheCatalog()
    {
        var failure = Assert.Single(_validator.Validate(Command() with { Email = "invalid" }).Errors);

        Assert.Equal(CommonErrors.InvalidEmail.Message, failure.ErrorMessage);
    }

    private static CreateCustomerCommand Command() =>
        new("Maria Souza", CustomerTestData.Cpf, CustomerTestData.EmailAddress, CustomerTestData.PhoneNumber, CustomerTestData.AddressInput());
}
