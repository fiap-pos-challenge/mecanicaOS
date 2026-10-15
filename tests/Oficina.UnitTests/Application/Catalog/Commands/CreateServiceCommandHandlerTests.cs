using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Catalog.Commands.CreateService;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.UnitTests.Application.Catalog.Commands;

public sealed class CreateServiceCommandHandlerTests
{
    private readonly IServiceRepository _services = Substitute.For<IServiceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateServiceCommandHandler _handler;

    public CreateServiceCommandHandlerTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new CreateServiceCommandHandler(_services, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsServiceAndCommits()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Troca de óleo", result.Value.Name);
        Assert.Equal("Inclui filtro", result.Value.Description);
        Assert.Equal(150.50m, result.Value.BasePrice);
        Assert.Equal(45, result.Value.EstimatedMinutes);
        _services.Received(1).Add(Arg.Is<Service>(service => service.Id == result.Value.Id && service.IsActive));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("A", 150, 45, "catalog.invalidName")]
    [InlineData("Troca de óleo", 0, 45, "catalog.invalidBasePrice")]
    [InlineData("Troca de óleo", -1, 45, "common.negativeAmount")]
    [InlineData("Troca de óleo", 150, 0, "catalog.invalidEstimatedMinutes")]
    public async Task Handle_InvalidData_ReturnsTheDomainErrorWithoutCommit(string name, decimal price, int minutes, string code)
    {
        var result = await _handler.Handle(Command() with { Name = name, BasePrice = price, EstimatedMinutes = minutes }, CancellationToken.None);

        Assert.Equal(code, result.Error!.Code);
        _services.DidNotReceive().Add(Arg.Any<Service>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.Unexpected));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(CommonErrors.Unexpected, result.Error);
    }

    [Fact]
    public async Task Handle_PriceRoundedToZero_ReturnsInvalidBasePrice()
    {
        var result = await _handler.Handle(Command() with { BasePrice = 0.001m }, CancellationToken.None);

        Assert.Equal(CatalogErrors.InvalidBasePrice, result.Error);
    }

    private static CreateServiceCommand Command() => new("Troca de óleo", "Inclui filtro", 150.50m, 45);
}
