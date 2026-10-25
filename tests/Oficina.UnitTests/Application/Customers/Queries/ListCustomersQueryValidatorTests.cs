using Oficina.Application.Customers.Queries.ListCustomers;

namespace Oficina.UnitTests.Application.Customers.Queries;

public sealed class ListCustomersQueryValidatorTests
{
    private readonly ListCustomersQueryValidator _validator = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 100)]
    public void Validate_PageInRange_HasNoErrors(int page, int pageSize)
    {
        Assert.True(_validator.Validate(new ListCustomersQuery(page, pageSize)).IsValid);
    }

    [Theory]
    [InlineData(0, 20, "Page", "common.invalidPage")]
    [InlineData(1, 0, "PageSize", "common.invalidPageSize")]
    [InlineData(1, 101, "PageSize", "common.invalidPageSize")]
    public void Validate_PageOutOfRange_ReturnsTheCatalogCode(int page, int pageSize, string field, string code)
    {
        var failure = Assert.Single(_validator.Validate(new ListCustomersQuery(page, pageSize)).Errors);

        Assert.Equal(field, failure.PropertyName);
        Assert.Equal(code, failure.ErrorCode);
    }
}
