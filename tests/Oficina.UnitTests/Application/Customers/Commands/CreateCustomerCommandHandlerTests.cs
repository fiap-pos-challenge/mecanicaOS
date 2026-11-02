using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Customers.Commands.CreateCustomer;
using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Enums;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Application.Customers.Commands;

public sealed class CreateCustomerCommandHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new CreateCustomerCommandHandler(_customers, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsCommitsAndReturnsTheCustomer()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Maria Souza", result.Value.Name);
        Assert.Equal(CustomerTestData.Cpf, result.Value.Document);
        Assert.Equal(DocumentType.Cpf, result.Value.DocumentType);
        Assert.Equal(CustomerTestData.EmailAddress, result.Value.Email);
        Assert.Equal(CustomerTestData.PhoneNumber, result.Value.Phone);
        Assert.Equal(new AddressDto("01310100", "Av. Paulista", "1000", "Sala 1", "Bela Vista", "São Paulo", "SP"), result.Value.Address);
        _customers.Received(1).Add(Arg.Is<Customer>(customer => customer.Id == result.Value.Id));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutOptionalFields_CreatesCustomerWithoutEmailAndAddress()
    {
        var result = await _handler.Handle(Command() with { Email = " ", Address = null }, CancellationToken.None);

        Assert.Null(result.Value.Email);
        Assert.Null(result.Value.Address);
    }

    [Fact]
    public async Task Handle_DocumentAlreadyRegistered_ReturnsDuplicateDocumentWithoutCommit()
    {
        _customers.GetByDocumentAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>()).Returns(CustomerTestData.Customer());

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(CustomerErrors.DuplicateDocument, result.Error);
        _customers.DidNotReceive().Add(Arg.Any<Customer>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("document", "customer.invalidDocument")]
    [InlineData("email", "common.invalidEmail")]
    [InlineData("phone", "customer.invalidPhone")]
    [InlineData("address", "customer.incompleteAddress")]
    [InlineData("name", "customer.invalidName")]
    public async Task Handle_InvalidData_ReturnsTheDomainErrorWithoutCommit(string invalidField, string expectedCode)
    {
        var command = invalidField switch
        {
            "document" => Command() with { Document = "11111111111" },
            "email" => Command() with { Email = "invalid" },
            "phone" => Command() with { Phone = "123" },
            "address" => Command() with { Address = CustomerTestData.AddressInput() with { City = null } },
            _ => Command() with { Name = "A" },
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(expectedCode, result.Error!.Code);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.DuplicateRecord));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(CommonErrors.DuplicateRecord, result.Error);
    }

    private static CreateCustomerCommand Command() =>
        new("Maria Souza", CustomerTestData.MaskedCpf, CustomerTestData.EmailAddress, CustomerTestData.PhoneNumber, CustomerTestData.AddressInput());
}
