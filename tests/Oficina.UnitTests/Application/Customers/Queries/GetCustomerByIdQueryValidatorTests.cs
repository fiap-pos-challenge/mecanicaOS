using Oficina.Application.Customers.Queries.GetCustomerById;

namespace Oficina.UnitTests.Application.Customers.Queries;

public sealed class GetCustomerByIdQueryValidatorTests
{
    private readonly GetCustomerByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_WithId_HasNoErrors()
    {
        Assert.True(_validator.Validate(new GetCustomerByIdQuery(Guid.CreateVersion7())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsRequiredField()
    {
        Assert.Equal("common.requiredField", Assert.Single(_validator.Validate(new GetCustomerByIdQuery(Guid.Empty)).Errors).ErrorCode);
    }
}
