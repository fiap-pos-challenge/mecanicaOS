using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Vehicles.Commands.CreateVehicle;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Application.Vehicles.Commands;

public sealed class CreateVehicleCommandHandlerTests
{
    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _timeProvider = new(DateTimeOffset.Parse("2026-06-15T00:00:00Z"));
    private readonly Customer _customer = CustomerTestData.Customer();
    private readonly CreateVehicleCommandHandler _handler;

    public CreateVehicleCommandHandlerTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _customers.GetByIdAsync(_customer.Id, Arg.Any<CancellationToken>()).Returns(CustomerTestData.Customer());
        _handler = new CreateVehicleCommandHandler(_vehicles, _customers, _unitOfWork, _timeProvider);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsCommitsAndReturnsTheVehicle()
    {
        var result = await _handler.Handle(Command() with{CustomerId = _customer.Id}, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(VehicleTestData.brand, result.Value.Brand);
        Assert.Equal(VehicleTestData.model, result.Value.Model);
        Assert.Equal(VehicleTestData.LicensePlate().Value, result.Value.LicensePlate);
        Assert.Equal(VehicleTestData.VehicleYear().Value, result.Value.Year);
        _vehicles.Received(1).Add(Arg.Is<Vehicle>(vehicle=>vehicle.Id == result.Value.Id));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VehicleAlreadyExistWithLicensePlate_ReturnsDuplicatedVehicle()
    {

        _vehicles.GetByLicensePlate(Arg.Any<LicensePlate>(), Arg.Any<CancellationToken>())
            .Returns(VehicleTestData.Vehicle());

        var result = await _handler.Handle(Command() with{CustomerId = _customer.Id}, CancellationToken.None);

        Assert.Equal(VehicleErrors.DuplicatedLicensePlate, result.Error);
        _vehicles.DidNotReceive().Add(Arg.Any<Vehicle>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerDoesNotExists_ReturnsCustomerDoesNotExists()
    {
        var result = await _handler.Handle(Command() with{ CustomerId = Guid.CreateVersion7()}, CancellationToken.None);
        Assert.Equal(CustomerErrors.NotFound,result.Error);
    }

    [Fact]
    public async Task Handle_CommitError_ReturnsCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(new Error("","", ErrorType.Unexpected)));

        var result = await _handler.Handle(Command() with{CustomerId = _customer.Id}, CancellationToken.None);

        Assert.NotNull(result.Error);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unexpected, result.Error.Type);
    }

    public CreateVehicleCommand Command()
    {
        return new(Guid.NewGuid(), VehicleTestData.LicensePlate().Value,VehicleTestData.brand , VehicleTestData.model,
            VehicleTestData.VehicleYear().Value);
    }

}
