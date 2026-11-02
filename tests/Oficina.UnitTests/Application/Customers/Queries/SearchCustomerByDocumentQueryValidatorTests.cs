using Oficina.Application.Customers.Queries.SearchCustomerByDocument;

namespace Oficina.UnitTests.Application.Customers.Queries;

public sealed class SearchCustomerByDocumentQueryValidatorTests
{
    private readonly SearchCustomerByDocumentQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidDocument_HasNoErrors()
    {
        Assert.True(_validator.Validate(new SearchCustomerByDocumentQuery(CustomerTestData.MaskedCpf)).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("11111111111")]
    public void Validate_InvalidDocument_ReturnsInvalidDocument(string document)
    {
        Assert.Equal("customer.invalidDocument", Assert.Single(_validator.Validate(new SearchCustomerByDocumentQuery(document)).Errors).ErrorCode);
    }
}
