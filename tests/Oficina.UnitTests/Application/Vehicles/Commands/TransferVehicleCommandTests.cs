using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Vehicles.Commands.TransferVehicle;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;

namespace Oficina.UnitTests.Application.Vehicles.Commands;

public sealed class TransferVehicleCommandTests
{
    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TransferVehicleCommandHandler _handler;
    private readonly Vehicle _vehicle = VehicleTestData.Vehicle();
    private readonly Customer _newOwner = CustomerTestData.Customer();

    public TransferVehicleCommandTests()
    {
        _handler = new TransferVehicleCommandHandler(_vehicles, _customers, _unitOfWork);
        _vehicles.GetByIdAsync(_vehicle.Id, Arg.Any<CancellationToken>()).Returns(_vehicle);
        _customers.GetByIdAsync(_newOwner.Id, Arg.Any<CancellationToken>()).Returns(_newOwner);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    [Fact]
    public async Task Handle_ValidTransfer_ChangesTheOwnerAndSaves()
    {
        var result = await _handler.Handle(new TransferVehicleCommand(_vehicle.Id, _newOwner.Id), CancellationToken.None);

        Assert.Equal(_newOwner.Id, result.Value.CustomerId);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownVehicle_ReturnsNotFound()
    {
        var result = await _handler.Handle(new TransferVehicleCommand(Guid.NewGuid(), _newOwner.Id), CancellationToken.None);

        Assert.Equal(VehicleErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_UnknownCustomer_ReturnsNotFound()
    {
        var result = await _handler.Handle(new TransferVehicleCommand(_vehicle.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(CustomerErrors.NotFound, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameOwner_ReturnsSameOwner()
    {
        var owner = CustomerTestData.Customer();
        _customers.GetByIdAsync(_vehicle.CustomerId, Arg.Any<CancellationToken>()).Returns(owner);

        var result = await _handler.Handle(new TransferVehicleCommand(_vehicle.Id, _vehicle.CustomerId), CancellationToken.None);

        Assert.Equal(VehicleErrors.SameOwner, result.Error);
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(VehicleErrors.DuplicatedLicensePlate));

        var result = await _handler.Handle(new TransferVehicleCommand(_vehicle.Id, _newOwner.Id), CancellationToken.None);

        Assert.Equal(VehicleErrors.DuplicatedLicensePlate, result.Error);
    }

    [Fact]
    public void Validate_EmptyIds_ReturnsTwoErrors() =>
        Assert.Equal(2, new TransferVehicleCommandValidator().Validate(new TransferVehicleCommand(Guid.Empty, Guid.Empty)).Errors.Count);
}
