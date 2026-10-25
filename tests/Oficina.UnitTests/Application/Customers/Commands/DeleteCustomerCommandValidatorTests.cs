using Oficina.Application.Customers.Commands.DeleteCustomer;

namespace Oficina.UnitTests.Application.Customers.Commands;

public sealed class DeleteCustomerCommandValidatorTests
{
    private readonly DeleteCustomerCommandValidator _validator = new();

    [Fact]
    public void Validate_WithId_HasNoErrors()
    {
        Assert.True(_validator.Validate(new DeleteCustomerCommand(Guid.CreateVersion7())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsRequiredField()
    {
        Assert.Equal("common.requiredField", Assert.Single(_validator.Validate(new DeleteCustomerCommand(Guid.Empty)).Errors).ErrorCode);
    }
}
