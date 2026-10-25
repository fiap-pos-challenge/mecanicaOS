using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Customers.Commands.RevealCustomerData;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Privacy.Entities;
using Oficina.Domain.Privacy.Enums;
using Oficina.Domain.Privacy.Repositories;

namespace Oficina.UnitTests.Application.Customers.Commands;

public sealed class RevealCustomerDataCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ISensitiveDataAccessRepository _accesses = Substitute.For<ISensitiveDataAccessRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Oficina.Domain.Customers.Entities.Customer _customer = CustomerTestData.Customer();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly RevealCustomerDataCommandHandler _handler;

    public RevealCustomerDataCommandHandlerTests()
    {
        _customers.GetByIdAsync(_customer.Id, Arg.Any<CancellationToken>()).Returns(_customer);
        _currentUser.UserId.Returns(_userId);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new RevealCustomerDataCommandHandler(_customers, _accesses, _currentUser, _unitOfWork, new FakeTimeProvider(Now));
    }

    [Theory]
    [InlineData(SensitiveField.Document, CustomerTestData.Cpf)]
    [InlineData(SensitiveField.Email, CustomerTestData.EmailAddress)]
    [InlineData(SensitiveField.Phone, CustomerTestData.PhoneNumber)]
    public async Task Handle_KnownCustomer_ReturnsTheOpenValueAndRecordsTheAccess(SensitiveField field, string expected)
    {
        var result = await _handler.Handle(new RevealCustomerDataCommand(_customer.Id, field, AccessReason.View), CancellationToken.None);

        Assert.Equal(expected, result.Value.Value);
        Assert.Equal(field, result.Value.Field);
        _accesses.Received(1).Add(Arg.Is<SensitiveDataAccess>(access =>
            access.UserId == _userId && access.CustomerId == _customer.Id && access.Field == field && access.Reason == AccessReason.View && access.OccurredAt == Now));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownCustomer_ReturnsNotFoundWithoutRecording()
    {
        var result = await _handler.Handle(new RevealCustomerDataCommand(Guid.CreateVersion7(), SensitiveField.Phone, AccessReason.Edit), CancellationToken.None);

        Assert.Equal(CustomerErrors.NotFound, result.Error);
        _accesses.DidNotReceive().Add(Arg.Any<SensitiveDataAccess>());
    }

    [Fact]
    public async Task Handle_WithoutUser_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _handler.Handle(new RevealCustomerDataCommand(_customer.Id, SensitiveField.Phone, AccessReason.View), CancellationToken.None);

        Assert.Equal(CommonErrors.Unauthorized, result.Error);
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheErrorWithoutTheValue()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await _handler.Handle(new RevealCustomerDataCommand(_customer.Id, SensitiveField.Document, AccessReason.View), CancellationToken.None);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
    }

    [Fact]
    public void Validator_RejectsEmptyIdAndUnknownEnums()
    {
        var validator = new RevealCustomerDataCommandValidator();

        Assert.False(validator.Validate(new RevealCustomerDataCommand(Guid.Empty, SensitiveField.Phone, AccessReason.View)).IsValid);
        Assert.False(validator.Validate(new RevealCustomerDataCommand(_customer.Id, (SensitiveField)9, AccessReason.View)).IsValid);
        Assert.True(validator.Validate(new RevealCustomerDataCommand(_customer.Id, SensitiveField.Email, AccessReason.Edit)).IsValid);
    }
}
