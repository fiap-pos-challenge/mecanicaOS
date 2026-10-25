using NSubstitute;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Application.Vehicles.Mappers;
using Oficina.Application.Vehicles.Queries;
using Oficina.Application.Vehicles.Queries.ListCustomerVehicles;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;

namespace Oficina.UnitTests.Application.Vehicles.Queries;

public sealed class ListCustomerVehiclesQueryTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IVehicleQueries _queries = Substitute.For<IVehicleQueries>();
    private readonly ListCustomerVehiclesQueryHandler _handler;

    public ListCustomerVehiclesQueryTests() => _handler = new ListCustomerVehiclesQueryHandler(_customers, _queries);

    [Fact]
    public async Task Handle_ExistingCustomer_ReturnsTheVehicles()
    {
        var customer = CustomerTestData.Customer();
        IReadOnlyList<VehicleDto> vehicles = [VehicleTestData.Vehicle().ToDto()];
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _queries.ListByCustomerAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(vehicles);

        var result = await _handler.Handle(new ListCustomerVehiclesQuery(customer.Id), CancellationToken.None);

        Assert.Equal(vehicles, result.Value.Items);
    }

    [Fact]
    public async Task Handle_UnknownCustomer_ReturnsNotFound()
    {
        _customers.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var result = await _handler.Handle(new ListCustomerVehiclesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(CustomerErrors.NotFound, result.Error);
    }

    [Fact]
    public void Validate_EmptyCustomer_ReturnsOneError() =>
        Assert.Single(new ListCustomerVehiclesQueryValidator().Validate(new ListCustomerVehiclesQuery(Guid.Empty)).Errors);
}
