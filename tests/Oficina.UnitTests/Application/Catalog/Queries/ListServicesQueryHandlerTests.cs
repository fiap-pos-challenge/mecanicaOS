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
    public static TheoryData<bool, CatalogStatus?, bool?> Filters => new()
    {
        { false, null, true },
        { true, null, null },
        { false, CatalogStatus.Inactive, false },
        { true, CatalogStatus.Active, true },
    };

    [Theory]
    [MemberData(nameof(Filters))]
    public async Task Handle_ReturnsThePageFromTheQueries(bool includeInactive, CatalogStatus? status, bool? expectedActive)
    {
        var queries = Substitute.For<IServiceQueries>();
        var page = new PagedResult<ServiceDto>(new[] { ServiceOrderBuilder.CreateService().ToDto() }, 2, 10, 11);
        queries.ListAsync(new PageRequest(2, 10), new CatalogFilter("óleo", expectedActive), Arg.Any<CancellationToken>()).Returns(page);

        var result = await new ListServicesQueryHandler(queries).Handle(new ListServicesQuery(2, 10, includeInactive, " óleo ", status), CancellationToken.None);

        Assert.Same(page, result.Value);
    }

    [Fact]
    public async Task Handle_BlankTerm_SearchesWithoutTerm()
    {
        var queries = Substitute.For<IServiceQueries>();

        await new ListServicesQueryHandler(queries).Handle(new ListServicesQuery(1, 10, false, "  "), CancellationToken.None);

        await queries.Received(1).ListAsync(new PageRequest(1, 10), new CatalogFilter(null, true), Arg.Any<CancellationToken>());
    }
}
