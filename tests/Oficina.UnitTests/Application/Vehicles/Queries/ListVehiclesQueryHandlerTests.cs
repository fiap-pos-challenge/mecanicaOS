using NSubstitute;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Application.Vehicles.Queries;
using Oficina.Application.Vehicles.Queries.ListVehicles;

namespace Oficina.UnitTests.Application.Vehicles.Queries;

public sealed class ListVehiclesQueryHandlerTests
{
    private readonly IVehicleQueries _queries = Substitute.For<IVehicleQueries>();
    private readonly ListVehiclesQueryHandler _handler;

    public ListVehiclesQueryHandlerTests() =>
        _handler = new ListVehiclesQueryHandler(_queries);

    [Fact]
    public async Task Handle_ListVehicles_ReturnsTheRequestedPage()
    {
        PagedResult<VehicleDto> vehicles = new(new List<VehicleDto>(), 2, 10, 0);
        _queries.ListAsync(Arg.Any<PageRequest>(), Arg.Any<CancellationToken>()).Returns(vehicles);

        var result = await _handler.Handle(new ListVehiclesQuery(2, 10), CancellationToken.None);

        Assert.Equal(2, result.Value.Page);
        Assert.Equal(10, result.Value.PageSize);
        await _queries.Received(1).ListAsync(Arg.Is<PageRequest>(page => page.Page == 2 && page.PageSize == 10), Arg.Any<CancellationToken>());
    }
}
