using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Customers.Commands.UpdateCustomer;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;

namespace Oficina.UnitTests.Application.Customers.Commands;

public sealed class UpdateCustomerCommandHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateCustomerCommandHandler _handler;
    private readonly Oficina.Domain.Customers.Entities.Customer _customer = CustomerTestData.Customer();

    public UpdateCustomerCommandHandlerTests()
    {
        _customers.GetByIdAsync(_customer.Id, Arg.Any<CancellationToken>()).Returns(_customer);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new UpdateCustomerCommandHandler(_customers, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ValidCommand_ChangesContactAndAddressAndCommits()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Maria Oliveira", _customer.Name);
        Assert.Null(_customer.Email);
        Assert.Equal("1133334444", _customer.Phone.Value);
        Assert.Equal("SP", _customer.Address!.State);
        Assert.Equal("Maria Oliveira", result.Value.Name);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutAddress_RemovesTheAddress()
    {
        await _handler.Handle(Command(), CancellationToken.None);

        var result = await _handler.Handle(Command() with { Address = null }, CancellationToken.None);

        Assert.Null(result.Value.Address);
        Assert.Null(_customer.Address);
    }

    [Fact]
    public async Task Handle_UnknownCustomer_ReturnsNotFoundWithoutCommit()
    {
        var result = await _handler.Handle(Command() with { Id = Guid.CreateVersion7() }, CancellationToken.None);

        Assert.Equal(CustomerErrors.NotFound, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("A", null, "1133334444", false, "customer.invalidName")]
    [InlineData("Maria", "invalid", "1133334444", false, "common.invalidEmail")]
    [InlineData("Maria", null, "12", false, "customer.invalidPhone")]
    [InlineData("Maria", null, "1133334444", true, "customer.incompleteAddress")]
    public async Task Handle_InvalidData_ReturnsTheDomainErrorWithoutCommit(string name, string? email, string phone, bool partialAddress, string code)
    {
        var address = partialAddress ? CustomerTestData.AddressInput() with { Number = null } : null;

        var result = await _handler.Handle(Command() with { Name = name, Email = email, Phone = phone, Address = address }, CancellationToken.None);

        Assert.Equal(code, result.Error!.Code);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
    }

    private UpdateCustomerCommand Command() =>
        new(_customer.Id, "Maria Oliveira", null, "(11) 3333-4444", CustomerTestData.AddressInput());
}
