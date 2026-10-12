using Oficina.Application.Vehicles.Commands.DeleteVehicle;
using Oficina.Domain.Common.Errors;

namespace Oficina.UnitTests.Application.Vehicles.Commands;

public class DeleteVehicleCommandValidatorTests
{
    private readonly DeleteVehicleCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidData_ReturnsNoErrors()
    {
        Assert.True(_validator.Validate(new DeleteVehicleCommand(Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Validate_InvalidData_ReturnsError()
    {
        var failure = Assert.Single(
            _validator.Validate(new DeleteVehicleCommand(Guid.Empty)).Errors);
        Assert.Equal(CommonErrors.RequiredField.Code, failure.ErrorCode);
    }

}
