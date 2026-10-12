using NSubstitute;
using Oficina.Application.Vehicles.Mappers;
using Oficina.Application.Vehicles.Queries;
using Oficina.Application.Vehicles.Queries.GetById;

namespace Oficina.UnitTests.Application.Vehicles.Queries;

public class GetVehicleByIdQueryHandlerTests
{
    private readonly IVehiclesQueries _queries = Substitute.For<IVehiclesQueries>();
    private readonly GetVehicleByIdQueryHandler _handler;

    public GetVehicleByIdQueryHandlerTests ()=>
        _handler = new GetVehicleByIdQueryHandler(_queries);

    [Fact]
    public async Task Handle_ExistingVehicle_ReturnsIt()
    {
        var vehicle = VehicleTestData.Vehicle().ToDto();
        _queries.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(vehicle);
        var query = new GetVehicleByIdQuery(vehicle.Id);
        var result = await _handler.Handle(query, CancellationToken.None);
        Assert.Equal(result.Value.Id, vehicle.Id);
    }
}
