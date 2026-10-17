using NSubstitute;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Application.ServiceOrders.Queries.GetAverageExecutionTime;

namespace Oficina.UnitTests.Application.ServiceOrders.Queries;

public sealed class GetAverageExecutionTimeQueryHandlerTests
{
    private readonly IServiceOrderQueries _queries = Substitute.For<IServiceOrderQueries>();
    private readonly GetAverageExecutionTimeQueryHandler _handler;

    public GetAverageExecutionTimeQueryHandlerTests() =>
        _handler = new GetAverageExecutionTimeQueryHandler(_queries);

    [Fact]
    public async Task Handle_WithPeriod_ReturnsTheAverageForThePeriod()
    {
        var from = new DateOnly(2026, 10, 1);
        var to = new DateOnly(2026, 10, 31);
        var average = new AverageExecutionTimeDto(135, 4);
        _queries.GetAverageExecutionTimeAsync(from, to, Arg.Any<CancellationToken>()).Returns(average);

        var result = await _handler.Handle(new GetAverageExecutionTimeQuery(from, to), CancellationToken.None);

        Assert.Equal(average, result.Value);
    }
}
