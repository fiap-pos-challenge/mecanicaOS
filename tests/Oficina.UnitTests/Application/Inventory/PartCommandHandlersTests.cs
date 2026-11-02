using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Inventory.Commands.CreatePart;
using Oficina.Application.Inventory.Commands.DeactivatePart;
using Oficina.Application.Inventory.Commands.ReactivatePart;
using Oficina.Application.Inventory.Commands.RegisterStockEntry;
using Oficina.Application.Inventory.Commands.UpdatePart;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Enums;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.Inventory;

public sealed class PartCommandHandlersTests
{
    private readonly IPartRepository _parts = Substitute.For<IPartRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Part _part = ServiceOrderBuilder.CreatePart(price: 25m, stock: 10m);

    public PartCommandHandlersTests()
    {
        _parts.GetByIdAsync(_part.Id, Arg.Any<CancellationToken>()).Returns(_part);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    [Fact]
    public async Task Create_ValidCommand_AddsActivePartWithZeroStock()
    {
        var result = await new CreatePartCommandHandler(_parts, _unitOfWork)
            .Handle(new CreatePartCommand("ole-5w30", "Óleo 5W30", PartType.Supply, "L", 42.5m, 4m), CancellationToken.None);

        Assert.Equal("OLE-5W30", result.Value.Code);
        Assert.Equal(0m, result.Value.StockQuantity);
        Assert.True(result.Value.IsActive);
        Assert.True(result.Value.IsBelowMinimum);
        _parts.Received(1).Add(Arg.Any<Part>());
    }

    [Fact]
    public async Task Create_InvalidCodeOrName_ReturnsTheDomainError()
    {
        var handler = new CreatePartCommandHandler(_parts, _unitOfWork);

        var invalidCode = await handler.Handle(new CreatePartCommand("código inválido", "Óleo", PartType.Supply, "L", 1m, 0m), CancellationToken.None);
        var invalidName = await handler.Handle(new CreatePartCommand("OLE-1", "x", PartType.Supply, "L", 1m, 0m), CancellationToken.None);

        Assert.Equal(InventoryErrors.InvalidPartCode, invalidCode.Error);
        Assert.Equal(InventoryErrors.InvalidName, invalidName.Error);
        _parts.DidNotReceive().Add(Arg.Any<Part>());
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.DuplicateRecord));

        var result = await new CreatePartCommandHandler(_parts, _unitOfWork)
            .Handle(new CreatePartCommand("FLT-001", "Filtro", PartType.Part, "un", 25m, 0m), CancellationToken.None);

        Assert.Equal(CommonErrors.DuplicateRecord, result.Error);
    }

    [Fact]
    public async Task Update_KnownPart_ChangesDetailsAndKeepsStock()
    {
        var result = await new UpdatePartCommandHandler(_parts, _unitOfWork)
            .Handle(new UpdatePartCommand(_part.Id, "Filtro Tecfil", PartType.Part, "un", 30m, 1m), CancellationToken.None);

        Assert.Equal("Filtro Tecfil", result.Value.Name);
        Assert.Equal(30m, result.Value.UnitPrice);
        Assert.Equal(10m, result.Value.StockQuantity);
    }

    [Fact]
    public async Task Update_UnknownPartOrInvalidData_ReturnsTheError()
    {
        var handler = new UpdatePartCommandHandler(_parts, _unitOfWork);

        var unknown = await handler.Handle(new UpdatePartCommand(Guid.CreateVersion7(), "Filtro", PartType.Part, "un", 30m, 1m), CancellationToken.None);
        var negativePrice = await handler.Handle(new UpdatePartCommand(_part.Id, "Filtro", PartType.Part, "un", -1m, 1m), CancellationToken.None);
        var invalidUnit = await handler.Handle(new UpdatePartCommand(_part.Id, "Filtro", PartType.Part, " ", 1m, 1m), CancellationToken.None);

        Assert.Equal(InventoryErrors.NotFound, unknown.Error);
        Assert.Equal(CommonErrors.NegativeAmount, negativePrice.Error);
        Assert.Equal(InventoryErrors.InvalidUnitOfMeasure, invalidUnit.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterStockEntry_KnownPart_AddsQuantityAndRecordsTheUser()
    {
        var userId = Guid.CreateVersion7();
        _currentUser.UserId.Returns(userId);

        var result = await new RegisterStockEntryCommandHandler(_parts, _currentUser, _unitOfWork)
            .Handle(new RegisterStockEntryCommand(_part.Id, 5m, "Compra NF 123"), CancellationToken.None);

        Assert.Equal(15m, result.Value.StockQuantity);
        Assert.Equal(userId, _part.StockMovements.Last().UserId);
        Assert.Equal(StockMovementType.Entry, _part.StockMovements.Last().Type);
    }

    [Fact]
    public async Task RegisterStockEntry_InvalidInput_ReturnsTheErrorWithoutCommit()
    {
        var handler = new RegisterStockEntryCommandHandler(_parts, _currentUser, _unitOfWork);

        var unknown = await handler.Handle(new RegisterStockEntryCommand(Guid.CreateVersion7(), 5m, "Compra"), CancellationToken.None);
        var zero = await handler.Handle(new RegisterStockEntryCommand(_part.Id, 0m, "Compra"), CancellationToken.None);
        var noReason = await handler.Handle(new RegisterStockEntryCommand(_part.Id, 5m, " "), CancellationToken.None);

        Assert.Equal(InventoryErrors.NotFound, unknown.Error);
        Assert.Equal(CommonErrors.InvalidQuantity, zero.Error);
        Assert.Equal(InventoryErrors.InvalidReason, noReason.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateAndReactivate_ToggleIsActive()
    {
        var deactivated = await new DeactivatePartCommandHandler(_parts, _unitOfWork).Handle(new DeactivatePartCommand(_part.Id), CancellationToken.None);
        Assert.True(deactivated.IsSuccess);
        Assert.False(_part.IsActive);

        var reactivated = await new ReactivatePartCommandHandler(_parts, _unitOfWork).Handle(new ReactivatePartCommand(_part.Id), CancellationToken.None);
        Assert.True(reactivated.IsSuccess);
        Assert.True(_part.IsActive);
    }

    [Fact]
    public async Task DeactivateAndReactivate_UnknownOrAlreadyInState_ReturnTheError()
    {
        var deactivate = new DeactivatePartCommandHandler(_parts, _unitOfWork);
        var reactivate = new ReactivatePartCommandHandler(_parts, _unitOfWork);

        Assert.Equal(InventoryErrors.NotFound, (await deactivate.Handle(new DeactivatePartCommand(Guid.CreateVersion7()), CancellationToken.None)).Error);
        Assert.Equal(InventoryErrors.NotFound, (await reactivate.Handle(new ReactivatePartCommand(Guid.CreateVersion7()), CancellationToken.None)).Error);
        Assert.Equal(InventoryErrors.AlreadyActive, (await reactivate.Handle(new ReactivatePartCommand(_part.Id), CancellationToken.None)).Error);
        await deactivate.Handle(new DeactivatePartCommand(_part.Id), CancellationToken.None);
        Assert.Equal(InventoryErrors.AlreadyInactive, (await deactivate.Handle(new DeactivatePartCommand(_part.Id), CancellationToken.None)).Error);
    }
}
