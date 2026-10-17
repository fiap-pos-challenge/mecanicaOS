using NSubstitute;
using Microsoft.Extensions.Time.Testing;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Catalog.Commands.UpdateService;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Application.Catalog.Dtos;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.Catalog.Commands;

public sealed class UpdateServiceCommandHandlerTests
{
    private readonly IServiceRepository _services = Substitute.For<IServiceRepository>();
    private readonly IPartRepository _parts = Substitute.For<IPartRepository>();
    private readonly IPriceChangeRepository _priceChanges = Substitute.For<IPriceChangeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly UpdateServiceCommandHandler _handler;
    private readonly Service _service = ServiceOrderBuilder.CreateService(price: 100m);

    public UpdateServiceCommandHandlerTests()
    {
        _services.GetByIdAsync(_service.Id, Arg.Any<CancellationToken>()).Returns(_service);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new UpdateServiceCommandHandler(_services, _parts, _priceChanges, _currentUser, _unitOfWork, new FakeTimeProvider());
        _parts.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Part>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ChangesDetailsAndPriceAndCommits()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alinhamento 3D", _service.Name);
        Assert.Equal("Com balanceamento", _service.Description);
        Assert.Equal(180m, _service.BasePrice.Amount);
        Assert.Equal(60, _service.EstimatedMinutes);
        Assert.Equal(180m, result.Value.BasePrice);
        Assert.Equal(15m, _service.CommissionPercent);
        Assert.Equal(180, _service.WarrantyDays);
        _priceChanges.Received(1).Add(Arg.Is<PriceChange>(change => change.OldValue == 100m && change.NewValue == 180m));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SamePrice_DoesNotRecordPriceChange()
    {
        var result = await _handler.Handle(Command() with { BasePrice = 100m }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _priceChanges.Received(1).Add(null);
    }

    [Fact]
    public async Task Handle_UnknownService_ReturnsNotFoundWithoutCommit()
    {
        var result = await _handler.Handle(Command() with { Id = Guid.CreateVersion7() }, CancellationToken.None);

        Assert.Equal(CatalogErrors.NotFound, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveService_ReturnsCannotChangeInactiveWithoutCommit()
    {
        _service.Deactivate();

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(CatalogErrors.CannotChangeInactive, result.Error);
        Assert.Equal("Troca de óleo", _service.Name);
        Assert.Equal(100m, _service.BasePrice.Amount);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("A", 180, 60, "catalog.invalidName")]
    [InlineData("Alinhamento 3D", 180, 0, "catalog.invalidEstimatedMinutes")]
    [InlineData("Alinhamento 3D", 0, 60, "catalog.invalidBasePrice")]
    [InlineData("Alinhamento 3D", -1, 60, "common.negativeAmount")]
    public async Task Handle_InvalidData_ReturnsTheDomainErrorWithoutCommit(string name, decimal price, int minutes, string code)
    {
        var result = await _handler.Handle(Command() with { Name = name, BasePrice = price, EstimatedMinutes = minutes }, CancellationToken.None);

        Assert.Equal(code, result.Error!.Code);
        Assert.Equal(100m, _service.BasePrice.Amount);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
    }

    [Fact]
    public async Task Handle_UnknownUsualPart_ReturnsInvalidUsualParts()
    {
        var result = await _handler.Handle(Command() with { UsualParts = [new UsualPartInput(Guid.CreateVersion7(), 1m)] }, CancellationToken.None);

        Assert.Equal(CatalogErrors.InvalidUsualParts, result.Error);
    }

    private UpdateServiceCommand Command() => new(_service.Id, "Alinhamento 3D", "Com balanceamento", 180m, 60, 15m, 180, []);
}
