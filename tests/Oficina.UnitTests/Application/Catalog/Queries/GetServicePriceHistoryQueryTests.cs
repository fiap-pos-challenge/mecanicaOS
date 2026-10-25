using NSubstitute;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Mappers;
using Oficina.Application.Catalog.Queries;
using Oficina.Application.Catalog.Queries.GetServicePriceHistory;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.Catalog.Queries;

public sealed class GetServicePriceHistoryQueryTests
{
    private readonly IServiceQueries _queries = Substitute.For<IServiceQueries>();
    private readonly IPriceHistoryQueries _history = Substitute.For<IPriceHistoryQueries>();

    [Fact]
    public async Task Handle_KnownService_ReturnsTheChanges()
    {
        var service = ServiceOrderBuilder.CreateService().ToDto();
        IReadOnlyList<PriceChangeDto> changes = [new("BasePrice", null, 100m, "Ana", DateTimeOffset.UnixEpoch)];
        _queries.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _history.ListAsync(CatalogItemKind.Service, service.Id, false, Arg.Any<CancellationToken>()).Returns(changes);

        var result = await new GetServicePriceHistoryQueryHandler(_queries, _history).Handle(new GetServicePriceHistoryQuery(service.Id), CancellationToken.None);

        Assert.Equal(changes, result.Value.Items);
    }

    [Fact]
    public async Task Handle_UnknownService_ReturnsNotFound()
    {
        _queries.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ServiceDto?)null);

        var result = await new GetServicePriceHistoryQueryHandler(_queries, _history).Handle(new GetServicePriceHistoryQuery(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(CatalogErrors.NotFound, result.Error);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsOneError() =>
        Assert.Single(new GetServicePriceHistoryQueryValidator().Validate(new GetServicePriceHistoryQuery(Guid.Empty)).Errors);
}
