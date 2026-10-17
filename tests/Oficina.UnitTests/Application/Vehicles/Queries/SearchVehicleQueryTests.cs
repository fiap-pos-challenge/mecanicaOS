using NSubstitute;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Application.Vehicles.Mappers;
using Oficina.Application.Vehicles.Queries;
using Oficina.Application.Vehicles.Queries.SearchVehicle;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Application.Vehicles.Queries;

public sealed class SearchVehicleQueryTests
{
    private readonly IVehicleQueries _queries = Substitute.For<IVehicleQueries>();
    private readonly SearchVehicleQueryHandler _handler;
    private readonly SearchVehicleQueryValidator _validator = new();

    public SearchVehicleQueryTests() => _handler = new SearchVehicleQueryHandler(_queries);

    [Fact]
    public async Task Handle_KnownPlate_ReturnsTheVehicle()
    {
        var vehicle = VehicleTestData.Vehicle().ToDto();
        _queries.FindByPlateAsync(Arg.Any<LicensePlate>(), Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await _handler.Handle(new SearchVehicleQuery("aaa-9999"), CancellationToken.None);

        Assert.Equal(vehicle, result.Value);
        await _queries.Received(1).FindByPlateAsync(Arg.Is<LicensePlate>(plate => plate.Value == "AAA9999"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownPlate_ReturnsNotFound()
    {
        _queries.FindByPlateAsync(Arg.Any<LicensePlate>(), Arg.Any<CancellationToken>()).Returns((VehicleDto?)null);

        var result = await _handler.Handle(new SearchVehicleQuery("AAA9999"), CancellationToken.None);

        Assert.Equal(VehicleErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_InvalidPlate_ReturnsValidationError()
    {
        var result = await _handler.Handle(new SearchVehicleQuery("invalid"), CancellationToken.None);

        Assert.Equal(VehicleErrors.InvalidLicensePlate, result.Error);
        await _queries.DidNotReceive().FindByPlateAsync(Arg.Any<LicensePlate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    public void Validate_InvalidPlate_ReturnsInvalidLicensePlate(string plate)
    {
        var failure = Assert.Single(_validator.Validate(new SearchVehicleQuery(plate)).Errors);
        Assert.Equal(VehicleErrors.InvalidLicensePlate.Code, failure.ErrorCode);
    }

    [Fact]
    public void Validate_ValidPlate_ReturnsNoErrors() =>
        Assert.True(_validator.Validate(new SearchVehicleQuery("ABC1D23")).IsValid);
}
