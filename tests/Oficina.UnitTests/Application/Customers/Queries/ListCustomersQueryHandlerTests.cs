using NSubstitute;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Dtos;
using Oficina.Application.Customers.Mappers;
using Oficina.Application.Customers.Queries;
using Oficina.Application.Customers.Queries.ListCustomers;

namespace Oficina.UnitTests.Application.Customers.Queries;

public sealed class ListCustomersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsThePageFromTheQueries()
    {
        var queries = Substitute.For<ICustomerQueries>();
        var page = new PagedResult<CustomerDto>(new[] { CustomerTestData.Customer().ToDto() }, 2, 10, 11);
        queries.ListAsync(new PageRequest(2, 10), Arg.Any<CancellationToken>()).Returns(page);

        var result = await new ListCustomersQueryHandler(queries).Handle(new ListCustomersQuery(2, 10), CancellationToken.None);

        Assert.Same(page, result.Value);
    }
}
