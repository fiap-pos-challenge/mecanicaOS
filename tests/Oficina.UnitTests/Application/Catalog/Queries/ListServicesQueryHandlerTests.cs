using NSubstitute;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Mappers;
using Oficina.Application.Catalog.Queries;
using Oficina.Application.Catalog.Queries.ListServices;
using Oficina.Application.Common.Pagination;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.Catalog.Queries;

public sealed class ListServicesQueryHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_ReturnsThePageFromTheQueries(bool includeInactive)
    {
        var queries = Substitute.For<IServiceQueries>();
        var page = new PagedResult<ServiceDto>(new[] { ServiceOrderBuilder.CreateService().ToDto() }, 2, 10, 11);
        queries.ListAsync(new PageRequest(2, 10), includeInactive, Arg.Any<CancellationToken>()).Returns(page);

        var result = await new ListServicesQueryHandler(queries).Handle(new ListServicesQuery(2, 10, includeInactive), CancellationToken.None);

        Assert.Same(page, result.Value);
    }
}
