using Oficina.Application.Users.Queries.GetUserById;

namespace Oficina.UnitTests.Application.Users.Queries;

public sealed class GetUserByIdQueryValidatorTests
{
    private readonly GetUserByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_WithId_HasNoErrors()
    {
        Assert.True(_validator.Validate(new GetUserByIdQuery(Guid.CreateVersion7())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsRequiredField()
    {
        var failure = Assert.Single(_validator.Validate(new GetUserByIdQuery(Guid.Empty)).Errors);

        Assert.Equal("Id", failure.PropertyName);
        Assert.Equal("common.requiredField", failure.ErrorCode);
    }
}
