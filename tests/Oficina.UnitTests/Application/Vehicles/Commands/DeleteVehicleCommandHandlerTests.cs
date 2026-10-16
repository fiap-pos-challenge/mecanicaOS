using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Vehicles.Commands.DeleteVehicle;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.Services;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.UnitTests.Application.Vehicles.Commands;

public class DeleteVehicleCommandHandlerTests
{

    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IServiceOrderRepository _serviceOrders = Substitute.For<IServiceOrderRepository>();
    private readonly Vehicle _vehicle = VehicleTestData.Vehicle();
    private readonly DeleteVehicleCommandHandler _handler;

    public DeleteVehicleCommandHandlerTests()
    {
        _vehicles.GetByIdAsync(_vehicle.Id, Arg.Any<CancellationToken>()).Returns(_vehicle);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new DeleteVehicleCommandHandler(_vehicles, new RegistrationDeletionPolicy(_serviceOrders, _vehicles), _unitOfWork);
    }

    [Fact]
    public async Task Handle_VehicleDeletion_ReturnsNoErrors()
    {
        var result = await _handler.Handle(new DeleteVehicleCommand(_vehicle.Id), CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_UnknownVehicleDeletion_ReturnsError()
    {
        var result = await _handler.Handle(new DeleteVehicleCommand(Guid.NewGuid()), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(VehicleErrors.NotFound, result.Error);
    }

}
