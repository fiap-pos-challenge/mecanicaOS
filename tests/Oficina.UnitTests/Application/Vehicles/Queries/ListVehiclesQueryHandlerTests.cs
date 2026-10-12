using NSubstitute;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Vehicles.Dto;
using Oficina.Application.Vehicles.Queries;
using Oficina.Application.Vehicles.Queries.ListVehicles;

namespace Oficina.UnitTests.Application.Vehicles.Queries;

public class ListVehiclesQueryHandlerTests
{
    private readonly IVehiclesQueries _queries = Substitute.For<IVehiclesQueries>();
    private readonly ListVehiclesQueryHandler _handler;

    public ListVehiclesQueryHandlerTests()
    {
        _handler = new ListVehiclesQueryHandler(_queries);
    }

    [Fact]
    public async Task Handle_ListVehicles_ReturnsVehiclesList()
    {
        List<VehicleDto> items = new();
        int page = 1;
        int pageSize = 10;
        PagedResult<VehicleDto> vehicles = new(items, page, pageSize, 0);
        _queries.ListAsync(Arg.Any<PageRequest>(), CancellationToken.None).Returns(vehicles);
        ListVehiclesQuery query = new(page, pageSize);
        var result = await _handler.Handle(query, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(page,result.Value.Page);
        Assert.Equal(pageSize,result.Value.PageSize);
    }

}
