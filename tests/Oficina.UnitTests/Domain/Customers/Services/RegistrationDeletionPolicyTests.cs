using NSubstitute;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.Services;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.UnitTests.Domain.Customers.Services;

public sealed class RegistrationDeletionPolicyTests
{
    private static readonly DateTimeOffset Today = new(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);
    private readonly IServiceOrderRepository _serviceOrders = Substitute.For<IServiceOrderRepository>();
    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly RegistrationDeletionPolicy _policy;

    public RegistrationDeletionPolicyTests() => _policy = new RegistrationDeletionPolicy(_serviceOrders, _vehicles);

    [Fact]
    public async Task EnsureCanDeleteCustomer_WithoutOpenOrder_Succeeds()
    {
        var customer = CreateCustomer();

        Assert.True((await _policy.EnsureCanDeleteCustomerAsync(customer, CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task EnsureCanDeleteCustomer_WithOpenOrder_ReturnsHasOpenServiceOrder()
    {
        var customer = CreateCustomer();
        _serviceOrders.HasOpenOrderForCustomerAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _policy.EnsureCanDeleteCustomerAsync(customer, CancellationToken.None);

        Assert.Equal(CustomerErrors.HasOpenServiceOrder, result.Error);
    }

    [Fact]
    public async Task EnsureCanDeleteCustomer_WithActiveVehicle_ReturnsHasActiveVehicles()
    {
        var customer = CreateCustomer();
        _vehicles.AnyForCustomerAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _policy.EnsureCanDeleteCustomerAsync(customer, CancellationToken.None);

        Assert.Equal(CustomerErrors.HasActiveVehicles, result.Error);
    }

    [Fact]
    public async Task EnsureCanDeleteVehicle_WithoutOpenOrder_Succeeds()
    {
        var vehicle = CreateVehicle();

        Assert.True((await _policy.EnsureCanDeleteVehicleAsync(vehicle, CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task EnsureCanDeleteVehicle_WithOpenOrder_ReturnsHasOpenServiceOrder()
    {
        var vehicle = CreateVehicle();
        _serviceOrders.HasOpenOrderForVehicleAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _policy.EnsureCanDeleteVehicleAsync(vehicle, CancellationToken.None);

        Assert.Equal(VehicleErrors.HasOpenServiceOrder, result.Error);
    }

    private static Customer CreateCustomer() =>
        Customer.Create("Maria Souza", Document.Create("52998224725").Value, null, Phone.Create("11987654321").Value, null).Value;

    private static Vehicle CreateVehicle() =>
        Vehicle.Create(Guid.CreateVersion7(), LicensePlate.Create("ABC1234").Value, "Fiat", "Uno", VehicleYear.Create(2010).Value, 0, Today).Value;
}
