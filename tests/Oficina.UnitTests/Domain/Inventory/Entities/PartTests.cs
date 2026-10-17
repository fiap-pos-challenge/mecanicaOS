using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Enums;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.ValueObjects;

namespace Oficina.UnitTests.Domain.Inventory.Entities;

public sealed class PartTests
{
    private static readonly PartCode Code = PartCode.Create("FLT-001").Value;
    private static readonly Money Price = Money.Create(35.9m).Value;
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public void Create_WithValidData_StartsActiveWithZeroStockAndNoMovements()
    {
        var result = Part.Create(Code, " Filtro de óleo ", PartType.Part, " un ", Price, Money.Zero, 5m, null, null);

        Assert.True(result.IsSuccess);
        var part = result.Value;
        Assert.NotEqual(Guid.Empty, part.Id);
        Assert.Equal(Code, part.Code);
        Assert.Equal("Filtro de óleo", part.Name);
        Assert.Equal(PartType.Part, part.Type);
        Assert.Equal("un", part.UnitOfMeasure);
        Assert.Equal(Price, part.UnitPrice);
        Assert.Equal(0m, part.StockQuantity);
        Assert.Equal(5m, part.MinimumStock);
        Assert.True(part.IsActive);
        Assert.Empty(part.StockMovements);
    }

    [Theory]
    [InlineData("")]
    [InlineData("F")]
    public void Create_WithTooShortName_ReturnsInvalidName(string name)
    {
        Assert.Equal(InventoryErrors.InvalidName, Part.Create(Code, name, PartType.Part, "un", Price, Money.Zero, 0m, null, null).Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("unidade-longa")]
    public void Create_WithInvalidUnit_ReturnsInvalidUnitOfMeasure(string unit)
    {
        Assert.Equal(InventoryErrors.InvalidUnitOfMeasure, Part.Create(Code, "Filtro", PartType.Part, unit, Price, Money.Zero, 0m, null, null).Error);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("0.0001")]
    public void Create_WithInvalidMinimumStock_ReturnsInvalidMinimumStock(string minimumStock)
    {
        var value = decimal.Parse(minimumStock, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(InventoryErrors.InvalidMinimumStock, Part.Create(Code, "Filtro", PartType.Supply, "l", Price, Money.Zero, value, null, null).Error);
    }

    [Fact]
    public void ChangeDetails_WithValidData_UpdatesDataButNotStock()
    {
        var part = CreatePartWithStock(10m);
        var newPrice = Money.Create(40m).Value;

        var result = part.ChangeDetails(Code, "Filtro de óleo premium", PartType.Part, "pc", newPrice, Money.Zero, 2m, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal("Filtro de óleo premium", part.Name);
        Assert.Equal("pc", part.UnitOfMeasure);
        Assert.Equal(newPrice, part.UnitPrice);
        Assert.Equal(2m, part.MinimumStock);
        Assert.Equal(10m, part.StockQuantity);
        Assert.Equal(Code, part.Code);
    }

    [Fact]
    public void ChangeDetails_WithCostSupplierAndBarcode_KeepsTrimmedValues()
    {
        var part = CreatePartWithStock(0m);
        var newCode = PartCode.Create("flt-002").Value;

        var result = part.ChangeDetails(newCode, "Filtro", PartType.Part, "un", Price, Money.Create(20m).Value, 1m, "  Auto Peças  ", " 7891234567895 ");

        Assert.True(result.IsSuccess);
        Assert.Equal("FLT-002", part.Code.Value);
        Assert.Equal(20m, part.Cost.Amount);
        Assert.Equal("Auto Peças", part.Supplier);
        Assert.Equal("7891234567895", part.Barcode);
    }

    [Theory]
    [InlineData("7891234567890")]
    [InlineData("789123456789")]
    [InlineData("789123456789A")]
    public void Create_WithInvalidBarcode_ReturnsInvalidBarcode(string barcode)
    {
        Assert.Equal(InventoryErrors.InvalidBarcode, Part.Create(Code, "Filtro", PartType.Part, "un", Price, Money.Zero, 0m, null, barcode).Error);
        Assert.False(Part.IsValidBarcode(barcode));
    }

    [Fact]
    public void Create_WithLongSupplier_ReturnsInvalidSupplier() =>
        Assert.Equal(InventoryErrors.InvalidSupplier, Part.Create(Code, "Filtro", PartType.Part, "un", Price, Money.Zero, 0m, new string('s', 121), null).Error);

    [Fact]
    public void Create_WithBlankSupplierAndBarcode_KeepsNull()
    {
        var part = Part.Create(Code, "Filtro", PartType.Part, "un", Price, Money.Zero, 0m, " ", "").Value;

        Assert.Null(part.Supplier);
        Assert.Null(part.Barcode);
    }

    [Fact]
    public void ChangeDetails_WithInvalidName_KeepsPreviousData()
    {
        var part = CreatePartWithStock(0m);

        var result = part.ChangeDetails(Code, "", PartType.Supply, "l", Price, Money.Zero, 1m, null, null);

        Assert.Equal(InventoryErrors.InvalidName, result.Error);
        Assert.Equal("Filtro de óleo", part.Name);
        Assert.Equal(PartType.Part, part.Type);
    }

    [Fact]
    public void RegisterEntry_AddsToStock_AndRecordsEntryMovement()
    {
        var part = CreatePartWithStock(0m);

        var result = part.RegisterEntry(Quantity.Create(12.5m).Value, " Compra NF 123 ", UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(12.5m, part.StockQuantity);
        var movement = Assert.Single(part.StockMovements);
        Assert.Equal(StockMovementType.Entry, movement.Type);
        Assert.Equal(12.5m, movement.Quantity.Value);
        Assert.Equal(12.5m, movement.BalanceAfter);
        Assert.Equal("Compra NF 123", movement.Reason);
        Assert.Equal(part.Id, movement.PartId);
        Assert.Equal(UserId, movement.UserId);
        Assert.Null(movement.ServiceOrderId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterEntry_WithoutReason_ReturnsInvalidReason(string reason)
    {
        var part = CreatePartWithStock(0m);

        var result = part.RegisterEntry(Quantity.Create(1m).Value, reason, UserId);

        Assert.Equal(InventoryErrors.InvalidReason, result.Error);
        Assert.Equal(0m, part.StockQuantity);
        Assert.Empty(part.StockMovements);
    }

    [Fact]
    public void HasAvailable_ComparesWithStock()
    {
        var part = CreatePartWithStock(3m);

        Assert.True(part.HasAvailable(Quantity.Create(3m).Value));
        Assert.False(part.HasAvailable(Quantity.Create(3.001m).Value));
    }

    [Fact]
    public void DeductForServiceOrder_WithEnoughStock_SubtractsAndRecordsDeduction()
    {
        var part = CreatePartWithStock(10m);
        var serviceOrderId = Guid.CreateVersion7();

        var result = part.DeductForServiceOrder(Quantity.Create(4m).Value, serviceOrderId, 42, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(6m, part.StockQuantity);
        var deduction = part.StockMovements.Last();
        Assert.Equal(StockMovementType.Deduction, deduction.Type);
        Assert.Equal(4m, deduction.Quantity.Value);
        Assert.Equal(6m, deduction.BalanceAfter);
        Assert.Equal(serviceOrderId, deduction.ServiceOrderId);
        Assert.Null(deduction.UserId);
        Assert.Equal("serviceOrder:42", deduction.Reason);
    }

    [Fact]
    public void DeductForServiceOrder_WithInsufficientStock_ReturnsInsufficientStockAndKeepsBalance()
    {
        var part = CreatePartWithStock(2m);

        var result = part.DeductForServiceOrder(Quantity.Create(2.5m).Value, Guid.CreateVersion7(), 7, UserId);

        Assert.Equal(InventoryErrors.InsufficientStock, result.Error);
        Assert.Equal(2m, part.StockQuantity);
        Assert.Single(part.StockMovements);
    }

    [Theory]
    [InlineData("4.999", true)]
    [InlineData("5", false)]
    [InlineData("8", false)]
    public void IsBelowMinimum_ComparesStockWithMinimum(string stock, bool expected)
    {
        var part = Part.Create(Code, "Filtro de óleo", PartType.Part, "un", Price, Money.Zero, 5m, null, null).Value;
        part.RegisterEntry(Quantity.Create(decimal.Parse(stock, System.Globalization.CultureInfo.InvariantCulture)).Value, "Carga inicial", UserId);

        Assert.Equal(expected, part.IsBelowMinimum);
    }

    [Fact]
    public void Deactivate_ThenReactivate_TogglesActive()
    {
        var part = CreatePartWithStock(0m);

        Assert.True(part.Deactivate().IsSuccess);
        Assert.False(part.IsActive);
        Assert.Equal(InventoryErrors.AlreadyInactive, part.Deactivate().Error);

        Assert.True(part.Reactivate().IsSuccess);
        Assert.True(part.IsActive);
        Assert.Equal(InventoryErrors.AlreadyActive, part.Reactivate().Error);
    }

    private static Part CreatePartWithStock(decimal stock)
    {
        var part = Part.Create(Code, "Filtro de óleo", PartType.Part, "un", Price, Money.Zero, 1m, null, null).Value;
        if (stock > 0)
        {
            part.RegisterEntry(Quantity.Create(stock).Value, "Carga inicial", UserId);
        }

        return part;
    }
}
