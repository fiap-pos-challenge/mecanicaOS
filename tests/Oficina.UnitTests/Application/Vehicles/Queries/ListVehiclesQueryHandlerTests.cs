using NSubstitute;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Application.Vehicles.Queries;
using Oficina.Application.Vehicles.Queries.ListVehicles;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Application.Vehicles.Queries;

public sealed class ListVehiclesQueryHandlerTests
{
    private readonly IVehicleQueries _queries = Substitute.For<IVehicleQueries>();
    private readonly ListVehiclesQueryHandler _handler;

    public ListVehiclesQueryHandlerTests() =>
        _handler = new ListVehiclesQueryHandler(_queries);

    [Fact]
    public async Task Handle_ListVehicles_ReturnsVehiclesList()
    {
        List<VehicleDto> items = new();
        const int page = 1;
        const int pageSize = 10;
        PagedResult<VehicleDto> vehicles = new(items, page, pageSize, 0);
        _queries.ListAsync(Arg.Any<PageRequest>(), Arg.Any<LicensePlate?>(), Arg.Any<CancellationToken>()).Returns(vehicles);

        var result = await _handler.Handle(new ListVehiclesQuery(page, pageSize, null), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(page, result.Value.Page);
        Assert.Equal(pageSize, result.Value.PageSize);
    }

    [Fact]
    public async Task Handle_WithValidPlate_FiltersByPlate()
    {
        PagedResult<VehicleDto> vehicles = new(new List<VehicleDto>(), 1, 10, 0);
        _queries.ListAsync(Arg.Any<PageRequest>(), Arg.Any<LicensePlate?>(), Arg.Any<CancellationToken>()).Returns(vehicles);

        var result = await _handler.Handle(new ListVehiclesQuery(1, 10, VehicleTestData.LicensePlate().Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _queries.Received(1).ListAsync(Arg.Any<PageRequest>(), Arg.Is<LicensePlate?>(plate => plate == VehicleTestData.LicensePlate()), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidPlate_ReturnsValidationError()
    {
        var result = await _handler.Handle(new ListVehiclesQuery(1, 10, "invalid"), CancellationToken.None);

        Assert.Equal(VehicleErrors.InvalidLicensePlate, result.Error);
        await _queries.DidNotReceive().ListAsync(Arg.Any<PageRequest>(), Arg.Any<LicensePlate?>(), Arg.Any<CancellationToken>());
    }
}
