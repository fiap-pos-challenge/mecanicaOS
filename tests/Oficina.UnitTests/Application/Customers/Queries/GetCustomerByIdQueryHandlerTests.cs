using NSubstitute;
using Oficina.Application.Customers.Mappers;
using Oficina.Application.Customers.Queries;
using Oficina.Application.Customers.Queries.GetCustomerById;
using Oficina.Domain.Customers.Errors;

namespace Oficina.UnitTests.Application.Customers.Queries;

public sealed class GetCustomerByIdQueryHandlerTests
{
    private readonly ICustomerQueries _queries = Substitute.For<ICustomerQueries>();
    private readonly GetCustomerByIdQueryHandler _handler;

    public GetCustomerByIdQueryHandlerTests() => _handler = new GetCustomerByIdQueryHandler(_queries);

    [Fact]
    public async Task Handle_ExistingCustomer_ReturnsIt()
    {
        var customer = CustomerTestData.Customer().ToDto();
        _queries.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _handler.Handle(new GetCustomerByIdQuery(customer.Id), CancellationToken.None);

        Assert.Same(customer, result.Value);
    }

    [Fact]
    public async Task Handle_UnknownCustomer_ReturnsNotFound()
    {
        var result = await _handler.Handle(new GetCustomerByIdQuery(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(CustomerErrors.NotFound, result.Error);
    }
}
