using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.ServiceOrders.Commands.OpenServiceOrder;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.Services;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.UnitTests.Application.ServiceOrders.Commands;

public sealed class OpenServiceOrderCommandHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly IServiceOrderRepository _serviceOrders = Substitute.For<IServiceOrderRepository>();
    private readonly IServiceOrderQueries _queries = Substitute.For<IServiceOrderQueries>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Customer _customer = CustomerTestData.Customer();
    private readonly Vehicle _vehicle;
    private readonly OpenServiceOrderCommandHandler _handler;

    public OpenServiceOrderCommandHandlerTests()
    {
        _vehicle = Vehicle.Create(_customer.Id, VehicleTestData.LicensePlate(), "Fiat", "Uno", VehicleTestData.VehicleYear(), TimeProvider.System.GetUtcNow()).Value;
        _customers.GetByDocumentAsync(Document.Create(CustomerTestData.Cpf).Value, Arg.Any<CancellationToken>()).Returns(_customer);
        _vehicles.GetByLicensePlate(_vehicle.LicensePlate, Arg.Any<CancellationToken>()).Returns(_vehicle);
        _serviceOrders.NextNumberAsync(Arg.Any<CancellationToken>()).Returns(ServiceOrderNumber.Create(1001).Value);
        _queries.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(call => ServiceOrderTestData.Detail(call.Arg<Guid>()));
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new OpenServiceOrderCommandHandler(
            _customers,
            _vehicles,
            _serviceOrders,
            new ServiceOrderOpeningPolicy(_serviceOrders),
            _queries,
            _currentUser,
            _unitOfWork,
            TimeProvider.System);
    }

    [Fact]
    public async Task Handle_CustomerAndOwnVehicle_OpensReceivedOrderAndReturnsTheDetail()
    {
        ServiceOrder? added = null;
        _serviceOrders.Add(Arg.Do<ServiceOrder>(order => added = order));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(added.Id, result.Value.Id);
        Assert.Equal(ServiceOrderStatus.Received, added.Status);
        Assert.Equal(_customer.Id, added.CustomerId);
        Assert.Equal(_vehicle.Id, added.VehicleId);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownCustomer_ReturnsCustomerNotFound()
    {
        var result = await _handler.Handle(Command() with { CustomerDocument = "11144477735" }, CancellationToken.None);

        Assert.Equal(CustomerErrors.NotFound, result.Error);
        _serviceOrders.DidNotReceive().Add(Arg.Any<ServiceOrder>());
    }

    [Fact]
    public async Task Handle_UnknownVehicle_ReturnsVehicleNotFound()
    {
        var result = await _handler.Handle(Command() with { LicensePlate = "BBB1C22" }, CancellationToken.None);

        Assert.Equal(VehicleErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_VehicleWithOpenOrder_ReturnsConflictWithoutConsumingANumber()
    {
        _serviceOrders.HasOpenOrderForVehicleAsync(_vehicle.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.VehicleHasOpenOrder, result.Error);
        await _serviceOrders.DidNotReceive().NextNumberAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidDocument_ReturnsTheValueObjectError()
    {
        var result = await _handler.Handle(Command() with { CustomerDocument = "123" }, CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.DuplicateRecord));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(CommonErrors.DuplicateRecord, result.Error);
    }

    private static OpenServiceOrderCommand Command() => new(CustomerTestData.Cpf, "AAA9999", "Barulho ao frear");
}
