using Oficina.Application.Catalog.Queries.GetServiceById;

namespace Oficina.UnitTests.Application.Catalog.Queries;

public sealed class GetServiceByIdQueryValidatorTests
{
    private readonly GetServiceByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_WithId_HasNoErrors()
    {
        Assert.True(_validator.Validate(new GetServiceByIdQuery(Guid.CreateVersion7())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsRequiredField()
    {
        Assert.Equal("common.requiredField", Assert.Single(_validator.Validate(new GetServiceByIdQuery(Guid.Empty)).Errors).ErrorCode);
    }
}
