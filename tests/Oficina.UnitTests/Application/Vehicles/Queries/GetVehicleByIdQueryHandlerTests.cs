using NSubstitute;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Application.Vehicles.Queries;
using Oficina.Application.Vehicles.Queries.GetVehicleById;
using Oficina.Domain.Customers.Errors;

namespace Oficina.UnitTests.Application.Vehicles.Queries;

public sealed class GetVehicleByIdQueryHandlerTests
{
    private readonly IVehicleQueries _queries = Substitute.For<IVehicleQueries>();
    private readonly GetVehicleByIdQueryHandler _handler;

    public GetVehicleByIdQueryHandlerTests() =>
        _handler = new GetVehicleByIdQueryHandler(_queries);

    [Fact]
    public async Task Handle_ExistingVehicle_ReturnsIt()
    {
        var vehicle = VehicleTestData.VehicleDetail();
        _queries.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(vehicle);
        var query = new GetVehicleByIdQuery(vehicle.Id);
        var result = await _handler.Handle(query, CancellationToken.None);
        Assert.Equal(result.Value.Id, vehicle.Id);
    }

    [Fact]
    public async Task Handle_UnknownVehicle_ReturnsNotFound()
    {
        _queries.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VehicleDetailDto?)null);
        var result = await _handler.Handle(new GetVehicleByIdQuery(Guid.CreateVersion7()), CancellationToken.None);
        Assert.Equal(VehicleErrors.NotFound, result.Error);
    }
}
