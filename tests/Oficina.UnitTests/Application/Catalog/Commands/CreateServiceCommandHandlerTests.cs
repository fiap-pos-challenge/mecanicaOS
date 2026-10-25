using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Catalog.Commands.CreateService;
using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Repositories;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.Catalog.Commands;

public sealed class CreateServiceCommandHandlerTests
{
    private readonly IServiceRepository _services = Substitute.For<IServiceRepository>();
    private readonly IPartRepository _parts = Substitute.For<IPartRepository>();
    private readonly IPriceChangeRepository _priceChanges = Substitute.For<IPriceChangeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly CreateServiceCommandHandler _handler;

    public CreateServiceCommandHandlerTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _currentUser.UserId.Returns(_userId);
        _handler = new CreateServiceCommandHandler(_services, _parts, _priceChanges, _currentUser, _unitOfWork, new FakeTimeProvider());
        _parts.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Part>());
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
        Assert.Equal(12.5m, result.Value.CommissionPercent);
        Assert.Equal(90, result.Value.WarrantyDays);
        _priceChanges.Received(1).Add(Arg.Is<PriceChange>(change =>
            change.ItemId == result.Value.Id && change.Kind == CatalogItemKind.Service && change.OldValue == null && change.NewValue == 150.50m && change.ChangedBy == _userId));
        _services.Received(1).Add(Arg.Is<Service>(service => service.Id == result.Value.Id && service.IsActive));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("A", 150, 45, "catalog.invalidName")]
    [InlineData("Troca de óleo", 0, 45, "catalog.invalidBasePrice")]
    [InlineData("Troca de óleo", -1, 45, "common.negativeAmount")]
    [InlineData("Troca de óleo", 150, 0, "catalog.invalidEstimatedMinutes")]
    [InlineData("Troca de óleo", 150, 45, "catalog.invalidCommissionPercent", 51)]
    [InlineData("Troca de óleo", 150, 45, "catalog.invalidWarrantyDays", 10, -1)]
    public async Task Handle_InvalidData_ReturnsTheDomainErrorWithoutCommit(string name, decimal price, int minutes, string code, decimal commission = 10, int warranty = 90)
    {
        var result = await _handler.Handle(Command() with { Name = name, BasePrice = price, EstimatedMinutes = minutes, CommissionPercent = commission, WarrantyDays = warranty }, CancellationToken.None);

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

    [Fact]
    public async Task Handle_WithUsualParts_KeepsThePartsOfTheService()
    {
        var part = ServiceOrderBuilder.CreatePart();
        _parts.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Part> { part });

        var result = await _handler.Handle(Command() with { UsualParts = [new UsualPartInput(part.Id, 2m)] }, CancellationToken.None);

        Assert.Equal(new UsualPartDto(part.Id, 2m), Assert.Single(result.Value.UsualParts));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public async Task Handle_InvalidUsualParts_ReturnsInvalidUsualPartsWithoutCommit(int knownParts, decimal quantity)
    {
        var part = ServiceOrderBuilder.CreatePart();
        _parts.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(knownParts == 1 ? new List<Part> { part } : new List<Part>());

        var result = await _handler.Handle(Command() with { UsualParts = [new UsualPartInput(part.Id, quantity)] }, CancellationToken.None);

        Assert.Equal(CatalogErrors.InvalidUsualParts, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static CreateServiceCommand Command() => new("Troca de óleo", "Inclui filtro", 150.50m, 45, 12.5m, 90, []);
}
