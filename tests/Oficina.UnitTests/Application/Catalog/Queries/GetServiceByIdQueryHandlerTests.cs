using NSubstitute;
using Oficina.Application.Catalog.Mappers;
using Oficina.Application.Catalog.Queries;
using Oficina.Application.Catalog.Queries.GetServiceById;
using Oficina.Domain.Catalog.Errors;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.Catalog.Queries;

public sealed class GetServiceByIdQueryHandlerTests
{
    private readonly IServiceQueries _queries = Substitute.For<IServiceQueries>();
    private readonly GetServiceByIdQueryHandler _handler;

    public GetServiceByIdQueryHandlerTests() => _handler = new GetServiceByIdQueryHandler(_queries);

    [Fact]
    public async Task Handle_ExistingService_ReturnsIt()
    {
        var service = ServiceOrderBuilder.CreateService().ToDto();
        _queries.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        var result = await _handler.Handle(new GetServiceByIdQuery(service.Id), CancellationToken.None);

        Assert.Same(service, result.Value);
    }

    [Fact]
    public async Task Handle_UnknownService_ReturnsNotFound()
    {
        var result = await _handler.Handle(new GetServiceByIdQuery(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(CatalogErrors.NotFound, result.Error);
    }
}
