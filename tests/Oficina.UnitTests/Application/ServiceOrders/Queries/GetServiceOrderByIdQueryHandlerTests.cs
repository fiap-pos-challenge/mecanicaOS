using NSubstitute;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Application.ServiceOrders.Queries.GetServiceOrderById;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.UnitTests.Application.ServiceOrders.Queries;

public sealed class GetServiceOrderByIdQueryHandlerTests
{
    private readonly IServiceOrderQueries _queries = Substitute.For<IServiceOrderQueries>();
    private readonly GetServiceOrderByIdQueryHandler _handler;

    public GetServiceOrderByIdQueryHandlerTests() =>
        _handler = new GetServiceOrderByIdQueryHandler(_queries);

    [Fact]
    public async Task Handle_ExistingServiceOrder_ReturnsIt()
    {
        var detail = ServiceOrderTestData.Detail(Guid.CreateVersion7());
        _queries.GetByIdAsync(detail.Id, Arg.Any<CancellationToken>()).Returns(detail);

        var result = await _handler.Handle(new GetServiceOrderByIdQuery(detail.Id), CancellationToken.None);

        Assert.Same(detail, result.Value);
    }

    [Fact]
    public async Task Handle_UnknownServiceOrder_ReturnsNotFound()
    {
        _queries.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ServiceOrderDetailDto?)null);

        var result = await _handler.Handle(new GetServiceOrderByIdQuery(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.NotFound, result.Error);
    }
}
