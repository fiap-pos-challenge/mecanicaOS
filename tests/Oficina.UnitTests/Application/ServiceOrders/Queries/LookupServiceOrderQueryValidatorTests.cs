using Oficina.Application.ServiceOrders.Queries.LookupServiceOrder;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.UnitTests.Application.ServiceOrders.Queries;

public sealed class LookupServiceOrderQueryValidatorTests
{
    private readonly LookupServiceOrderQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidNumberAndDocument_ReturnsNoErrors()
    {
        Assert.True(_validator.Validate(new LookupServiceOrderQuery(1001, CustomerTestData.MaskedCpf)).IsValid);
    }

    [Fact]
    public void Validate_NotPositiveNumber_ReturnsInvalidNumber()
    {
        var failure = Assert.Single(_validator.Validate(new LookupServiceOrderQuery(0, CustomerTestData.Cpf)).Errors);
        Assert.Equal(ServiceOrderErrors.InvalidNumber.Code, failure.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678900")]
    public void Validate_InvalidDocument_ReturnsInvalidDocument(string document)
    {
        var failure = Assert.Single(_validator.Validate(new LookupServiceOrderQuery(1001, document)).Errors);
        Assert.Equal(CustomerErrors.InvalidDocument.Code, failure.ErrorCode);
    }
}
