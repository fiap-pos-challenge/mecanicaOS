using Oficina.Application.Customers.Commands.UpdateCustomer;

namespace Oficina.UnitTests.Application.Customers.Commands;

public sealed class UpdateCustomerCommandValidatorTests
{
    private readonly UpdateCustomerCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        Assert.True(_validator.Validate(Command()).IsValid);
    }

    [Theory]
    [InlineData("Id", "common.requiredField")]
    [InlineData("Name", "customer.invalidName")]
    [InlineData("Email", "common.invalidEmail")]
    [InlineData("Phone", "customer.invalidPhone")]
    [InlineData("Address", "customer.invalidZipCode")]
    public void Validate_InvalidField_ReturnsTheCatalogCode(string field, string code)
    {
        var command = field switch
        {
            "Id" => Command() with { Id = Guid.Empty },
            "Name" => Command() with { Name = new string('a', 161) },
            "Email" => Command() with { Email = "invalid" },
            "Phone" => Command() with { Phone = string.Empty },
            _ => Command() with { Address = CustomerTestData.AddressInput() with { ZipCode = "123" } },
        };

        var failure = Assert.Single(_validator.Validate(command).Errors);

        Assert.Equal(field, failure.PropertyName);
        Assert.Equal(code, failure.ErrorCode);
    }

    private static UpdateCustomerCommand Command() =>
        new(Guid.CreateVersion7(), "Maria Souza", CustomerTestData.EmailAddress, CustomerTestData.PhoneNumber, null);
}
