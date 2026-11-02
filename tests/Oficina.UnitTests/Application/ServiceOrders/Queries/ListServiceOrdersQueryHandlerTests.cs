using NSubstitute;
using Oficina.Application.Common.Pagination;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Application.ServiceOrders.Queries.ListServiceOrders;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.UnitTests.Application.ServiceOrders.Queries;

public sealed class ListServiceOrdersQueryHandlerTests
{
    private readonly IServiceOrderQueries _queries = Substitute.For<IServiceOrderQueries>();
    private readonly ListServiceOrdersQueryHandler _handler;

    public ListServiceOrdersQueryHandlerTests()
    {
        _handler = new ListServiceOrdersQueryHandler(_queries);
        _queries.ListAsync(Arg.Any<PageRequest>(), Arg.Any<ServiceOrderStatus?>(), Arg.Any<LicensePlate?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ServiceOrderSummaryDto>(new List<ServiceOrderSummaryDto>(), 2, 10, 0));
    }

    [Fact]
    public async Task Handle_WithoutFilters_ReturnsThePage()
    {
        var result = await _handler.Handle(new ListServiceOrdersQuery(2, 10, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Page);
        await _queries.Received(1).ListAsync(
            Arg.Is<PageRequest>(page => page.Page == 2 && page.PageSize == 10),
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithStatusAndPlate_PassesBothFilters()
    {
        var result = await _handler.Handle(new ListServiceOrdersQuery(1, 10, "inprogress", VehicleTestData.LicensePlate().Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _queries.Received(1).ListAsync(
            Arg.Any<PageRequest>(),
            ServiceOrderStatus.InProgress,
            Arg.Is<LicensePlate?>(plate => plate == VehicleTestData.LicensePlate()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownStatus_ReturnsInvalidStatus()
    {
        var result = await _handler.Handle(new ListServiceOrdersQuery(1, 10, "Cancelled", null), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.InvalidStatus, result.Error);
        await _queries.DidNotReceive().ListAsync(Arg.Any<PageRequest>(), Arg.Any<ServiceOrderStatus?>(), Arg.Any<LicensePlate?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidPlate_ReturnsInvalidLicensePlate()
    {
        var result = await _handler.Handle(new ListServiceOrdersQuery(1, 10, null, "invalid"), CancellationToken.None);

        Assert.Equal(VehicleErrors.InvalidLicensePlate, result.Error);
        await _queries.DidNotReceive().ListAsync(Arg.Any<PageRequest>(), Arg.Any<ServiceOrderStatus?>(), Arg.Any<LicensePlate?>(), Arg.Any<CancellationToken>());
    }
}
