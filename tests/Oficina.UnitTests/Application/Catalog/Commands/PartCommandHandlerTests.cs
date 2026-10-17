using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Catalog.Commands.CreatePart;
using Oficina.Application.Catalog.Commands.DeactivatePart;
using Oficina.Application.Catalog.Commands.ReactivatePart;
using Oficina.Application.Catalog.Commands.UpdatePart;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Enums;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.Inventory.ValueObjects;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.Catalog.Commands;

public sealed class PartCommandHandlerTests
{
    private readonly IPartRepository _parts = Substitute.For<IPartRepository>();
    private readonly IPackageRepository _packages = Substitute.For<IPackageRepository>();
    private readonly IPriceChangeRepository _priceChanges = Substitute.For<IPriceChangeRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Part _part = ServiceOrderBuilder.CreatePart(price: 25m);

    public PartCommandHandlerTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _parts.GetByIdAsync(_part.Id, Arg.Any<CancellationToken>()).Returns(_part);
        _packages.ListActiveNamesUsingAsync(Arg.Any<CatalogItemKind>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<string>());
    }

    private CreatePartCommandHandler Create() => new(_parts, _priceChanges, _currentUser, _unitOfWork, new FakeTimeProvider());

    private UpdatePartCommandHandler Update() => new(_parts, _priceChanges, _currentUser, _unitOfWork, new FakeTimeProvider());

    private static CreatePartCommand NewPart() => new("pec-9001", "Correia dentada", PartType.Part, "un", 150m, 90m, 2m, "Auto Peças", "7891234567895");

    [Fact]
    public async Task Create_ValidCommand_AddsPartWithCostAndRecordsBothPrices()
    {
        var result = await Create().Handle(NewPart(), CancellationToken.None);

        Assert.Equal("PEC-9001", result.Value.Code);
        Assert.Equal(90m, result.Value.Cost);
        _parts.Received(1).Add(Arg.Is<Part>(part => part.Id == result.Value.Id));
        _priceChanges.Received(1).Add(Arg.Is<PriceChange>(change => change.Field == PriceField.Cost && change.NewValue == 90m));
        _priceChanges.Received(1).Add(Arg.Is<PriceChange>(change => change.Field == PriceField.UnitPrice && change.NewValue == 150m));
    }

    [Fact]
    public async Task Create_DuplicatedCode_ReturnsDuplicatePartCode()
    {
        _parts.CodeExistsAsync(Arg.Any<PartCode>(), null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Create().Handle(NewPart(), CancellationToken.None);

        Assert.Equal(InventoryErrors.DuplicatePartCode, result.Error);
        _parts.DidNotReceive().Add(Arg.Any<Part>());
    }

    [Theory]
    [InlineData("pe ça", 1, 1, "", "inventory.invalidPartCode")]
    [InlineData("PEC-1", -1, 1, "", "common.negativeAmount")]
    [InlineData("PEC-1", 1, -1, "", "common.negativeAmount")]
    [InlineData("PEC-1", 1, 1, "123", "inventory.invalidBarcode")]
    public async Task Create_InvalidValues_ReturnsTheErrorWithoutCommit(string code, decimal price, decimal cost, string barcode, string expected)
    {
        var result = await Create().Handle(NewPart() with { Code = code, UnitPrice = price, Cost = cost, Barcode = barcode }, CancellationToken.None);

        Assert.Equal(expected, result.Error!.Code);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_CommitFails_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.Unexpected));

        Assert.Equal(CommonErrors.Unexpected, (await Create().Handle(NewPart(), CancellationToken.None)).Error);
    }

    [Fact]
    public async Task Update_ValidCommand_ChangesThePartAndRecordsTheNewPrice()
    {
        var command = new UpdatePartCommand(_part.Id, "FLT-009", "Filtro", PartType.Supply, "l", 30m, 12m, 1m, null, null);

        var result = await Update().Handle(command, CancellationToken.None);

        Assert.Equal("FLT-009", result.Value.Code);
        Assert.Equal(30m, _part.UnitPrice.Amount);
        _priceChanges.Received(1).Add(Arg.Is<PriceChange>(change => change.Field == PriceField.UnitPrice && change.OldValue == 25m && change.NewValue == 30m));
    }

    [Fact]
    public async Task Update_UnknownPart_ReturnsNotFound()
    {
        var command = new UpdatePartCommand(Guid.CreateVersion7(), "FLT-009", "Filtro", PartType.Part, "un", 30m, 12m, 1m, null, null);

        Assert.Equal(InventoryErrors.NotFound, (await Update().Handle(command, CancellationToken.None)).Error);
    }

    [Theory]
    [InlineData(true, "", "inventory.duplicatePartCode")]
    [InlineData(false, "x", "inventory.invalidPartCode")]
    [InlineData(false, "FLT-1", "inventory.invalidName")]
    public async Task Update_InvalidData_ReturnsTheErrorWithoutCommit(bool duplicated, string code, string expected)
    {
        _parts.CodeExistsAsync(Arg.Any<PartCode>(), _part.Id, Arg.Any<CancellationToken>()).Returns(duplicated);
        var command = new UpdatePartCommand(_part.Id, duplicated ? "FLT-002" : code == "x" ? "x y" : code, expected == "inventory.invalidName" ? "" : "Filtro", PartType.Part, "un", 30m, 12m, 1m, null, null);

        var result = await Update().Handle(command, CancellationToken.None);

        Assert.Equal(expected, result.Error!.Code);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_CommitFails_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));
        var command = new UpdatePartCommand(_part.Id, "FLT-009", "Filtro", PartType.Part, "un", 30m, 12m, 1m, null, null);

        Assert.Equal(CommonErrors.ConcurrencyConflict, (await Update().Handle(command, CancellationToken.None)).Error);
    }

    [Fact]
    public async Task Deactivate_PartInActivePackage_ReturnsUsedByActivePackage()
    {
        _packages.ListActiveNamesUsingAsync(CatalogItemKind.Part, _part.Id, Arg.Any<CancellationToken>()).Returns(new List<string> { "Revisão" });

        var result = await new DeactivatePartCommandHandler(_parts, _packages, _unitOfWork).Handle(new DeactivatePartCommand(_part.Id), CancellationToken.None);

        Assert.Equal(CatalogErrors.UsedByActivePackageCode, result.Error!.Code);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateAndReactivate_TogglesTheStatus()
    {
        var deactivate = new DeactivatePartCommandHandler(_parts, _packages, _unitOfWork);
        var reactivate = new ReactivatePartCommandHandler(_parts, _unitOfWork);

        Assert.True((await deactivate.Handle(new DeactivatePartCommand(_part.Id), CancellationToken.None)).IsSuccess);
        Assert.Equal(InventoryErrors.AlreadyInactive, (await deactivate.Handle(new DeactivatePartCommand(_part.Id), CancellationToken.None)).Error);
        Assert.True((await reactivate.Handle(new ReactivatePartCommand(_part.Id), CancellationToken.None)).IsSuccess);
        Assert.Equal(InventoryErrors.AlreadyActive, (await reactivate.Handle(new ReactivatePartCommand(_part.Id), CancellationToken.None)).Error);
        Assert.Equal(InventoryErrors.NotFound, (await deactivate.Handle(new DeactivatePartCommand(Guid.CreateVersion7()), CancellationToken.None)).Error);
        Assert.Equal(InventoryErrors.NotFound, (await reactivate.Handle(new ReactivatePartCommand(Guid.CreateVersion7()), CancellationToken.None)).Error);
    }

    [Fact]
    public void Validators_ReportEveryInvalidField()
    {
        var invalid = new CreatePartCommand("x y", "", (PartType)9, "", -1m, -1m, -0.0001m, new string('s', 121), "1");

        var codes = new CreatePartCommandValidator().Validate(invalid).Errors.Select(error => error.ErrorCode).ToArray();

        Assert.Equal(9, codes.Length);
        Assert.True(new CreatePartCommandValidator().Validate(NewPart()).IsValid);
        Assert.Single(new UpdatePartCommandValidator().Validate(new UpdatePartCommand(Guid.Empty, "PEC-1", "Filtro", PartType.Part, "un", 1m, 1m, 0m, null, null)).Errors);
        Assert.Single(new DeactivatePartCommandValidator().Validate(new DeactivatePartCommand(Guid.Empty)).Errors);
        Assert.Single(new ReactivatePartCommandValidator().Validate(new ReactivatePartCommand(Guid.Empty)).Errors);
    }
}
