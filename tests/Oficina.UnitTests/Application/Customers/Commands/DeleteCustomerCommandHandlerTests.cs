using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Customers.Commands.DeleteCustomer;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.Services;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.UnitTests.Application.Customers.Commands;

public sealed class DeleteCustomerCommandHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly IServiceOrderRepository _serviceOrders = Substitute.For<IServiceOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteCustomerCommandHandler _handler;
    private readonly Customer _customer = CustomerTestData.Customer();

    public DeleteCustomerCommandHandlerTests()
    {
        _customers.GetByIdAsync(_customer.Id, Arg.Any<CancellationToken>()).Returns(_customer);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new DeleteCustomerCommandHandler(_customers, new RegistrationDeletionPolicy(_serviceOrders, _vehicles), _unitOfWork);
    }

    [Fact]
    public async Task Handle_CustomerWithoutOpenOrderOrVehicle_RemovesAndCommits()
    {
        var result = await _handler.Handle(new DeleteCustomerCommand(_customer.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _customers.Received(1).Remove(_customer);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownCustomer_ReturnsNotFound()
    {
        var result = await _handler.Handle(new DeleteCustomerCommand(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(CustomerErrors.NotFound, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerWithOpenServiceOrder_IsBlocked()
    {
        _serviceOrders.HasOpenOrderForCustomerAsync(_customer.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new DeleteCustomerCommand(_customer.Id), CancellationToken.None);

        Assert.Equal(CustomerErrors.HasOpenServiceOrder, result.Error);
        _customers.DidNotReceive().Remove(Arg.Any<Customer>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerWithActiveVehicle_IsBlocked()
    {
        _vehicles.AnyForCustomerAsync(_customer.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new DeleteCustomerCommand(_customer.Id), CancellationToken.None);

        Assert.Equal(CustomerErrors.HasActiveVehicles, result.Error);
        _customers.DidNotReceive().Remove(Arg.Any<Customer>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await _handler.Handle(new DeleteCustomerCommand(_customer.Id), CancellationToken.None);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
    }
}
