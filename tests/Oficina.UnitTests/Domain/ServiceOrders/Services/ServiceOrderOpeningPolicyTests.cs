using NSubstitute;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.Services;

namespace Oficina.UnitTests.Domain.ServiceOrders.Services;

public sealed class ServiceOrderOpeningPolicyTests
{
    private static readonly DateTimeOffset Today = new(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);
    private readonly IServiceOrderRepository _serviceOrders = Substitute.For<IServiceOrderRepository>();
    private readonly ServiceOrderOpeningPolicy _policy;

    public ServiceOrderOpeningPolicyTests() => _policy = new ServiceOrderOpeningPolicy(_serviceOrders);

    [Fact]
    public async Task EnsureCanOpen_VehicleOfTheCustomerWithoutOpenOrder_Succeeds()
    {
        var customer = CreateCustomer();
        var vehicle = CreateVehicle(customer.Id);

        var result = await _policy.EnsureCanOpenAsync(customer, vehicle, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureCanOpen_VehicleWithOpenOrder_ReturnsVehicleHasOpenOrder()
    {
        var customer = CreateCustomer();
        var vehicle = CreateVehicle(customer.Id);
        _serviceOrders.HasOpenOrderForVehicleAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _policy.EnsureCanOpenAsync(customer, vehicle, CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.VehicleHasOpenOrder, result.Error);
    }

    [Fact]
    public async Task EnsureCanOpen_VehicleOfAnotherCustomer_ReturnsVehicleNotOwnedByCustomer()
    {
        var customer = CreateCustomer();
        var vehicle = CreateVehicle(Guid.CreateVersion7());

        var result = await _policy.EnsureCanOpenAsync(customer, vehicle, CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.VehicleNotOwnedByCustomer, result.Error);
        await _serviceOrders.DidNotReceive().HasOpenOrderForVehicleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static Customer CreateCustomer() =>
        Customer.Create("Maria Souza", Document.Create("52998224725").Value, Email.Create("maria@exemplo.com").Value, Phone.Create("11987654321").Value, null).Value;

    private static Vehicle CreateVehicle(Guid customerId) =>
        Vehicle.Create(customerId, LicensePlate.Create("BRA2E19").Value, "Volkswagen", "Gol", VehicleYear.Create(2020).Value, Today).Value;
}
