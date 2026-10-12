using Oficina.Application.Vehicles.Queries.GetById;

namespace Oficina.UnitTests.Application.Vehicles.Queries;

public sealed class GetVehicleByIdQueryValidatorTests
{
    private readonly GetVehicleByIdValidator _validator = new();

    [Fact]
    public void Validate_WithId_HasNoErrors()
    {
        Assert.True(_validator.Validate(new GetVehicleByIdQuery(Guid.CreateVersion7())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsRequiredField()
    {
        Assert.Equal("common.requiredField", Assert.Single(_validator.Validate(new GetVehicleByIdQuery(Guid.Empty)).Errors).ErrorCode);
    }
}
