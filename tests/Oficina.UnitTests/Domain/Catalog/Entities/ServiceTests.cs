using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.UnitTests.Domain.Catalog.Entities;

public sealed class ServiceTests
{
    [Theory]
    [InlineData(-1, 90)]
    [InlineData(50.01, 90)]
    public void Create_InvalidCommission_ReturnsInvalidCommissionPercent(double commission, int warranty)
    {
        Assert.Equal(CatalogErrors.InvalidCommissionPercent, Service.Create("Alinhamento", null, Price, 30, (decimal)commission, warranty).Error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3651)]
    public void ChangeDetails_InvalidWarranty_ReturnsInvalidWarrantyDays(int warranty)
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;

        Assert.Equal(CatalogErrors.InvalidWarrantyDays, service.ChangeDetails("Alinhamento", null, 30, 10m, warranty).Error);
        Assert.Equal(90, service.WarrantyDays);
    }

    [Fact]
    public void ChangeDetails_ValidTerms_ChangesCommissionAndWarranty()
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;

        Assert.True(service.ChangeDetails("Alinhamento", null, 30, 0m, 0).IsSuccess);
        Assert.Equal(0m, service.CommissionPercent);
        Assert.Equal(0, service.WarrantyDays);
    }

    [Fact]
    public void PriceChange_Record_KeepsTheValuesAndSkipsTheSamePrice()
    {
        var serviceId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        var change = PriceChange.Record(CatalogItemKind.Service, serviceId, PriceField.BasePrice, Price, Money.Create(200m).Value, userId, DateTimeOffset.UnixEpoch);

        Assert.NotNull(change);
        Assert.Equal(CatalogItemKind.Service, change.Kind);
        Assert.Equal(PriceField.BasePrice, change.Field);
        Assert.Equal(serviceId, change.ItemId);
        Assert.Equal(Price.Amount, change.OldValue);
        Assert.Equal(200m, change.NewValue);
        Assert.Equal(userId, change.ChangedBy);
        Assert.Equal(DateTimeOffset.UnixEpoch, change.ChangedAt);
        Assert.NotEqual(Guid.Empty, change.Id);
        Assert.Null(PriceChange.Record(CatalogItemKind.Service, serviceId, PriceField.BasePrice, Price, Price, userId, DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void ReplaceUsualParts_KeepsUniquePartsAndRejectsRepeatedOrInactive()
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;
        var partId = Guid.CreateVersion7();
        var one = Quantity.Create(1m).Value;

        Assert.True(service.ReplaceUsualParts([(partId, one)]).IsSuccess);
        Assert.Equal(partId, Assert.Single(service.UsualParts).PartId);
        Assert.Equal(CatalogErrors.InvalidUsualParts, service.ReplaceUsualParts([(partId, one), (partId, one)]).Error);
        Assert.Equal(CatalogErrors.InvalidUsualParts, service.ReplaceUsualParts([(Guid.Empty, one)]).Error);
        service.Deactivate();
        Assert.Equal(CatalogErrors.CannotChangeInactive, service.ReplaceUsualParts([]).Error);
    }

    private static readonly Money Price = Money.Create(150m).Value;

    [Fact]
    public void Create_WithValidData_IsActiveWithCopiedFields()
    {
        var result = Service.Create(" Troca de óleo ", " Inclui filtro ", Price, 45, 10m, 90);

        Assert.True(result.IsSuccess);
        var service = result.Value;
        Assert.NotEqual(Guid.Empty, service.Id);
        Assert.Equal("Troca de óleo", service.Name);
        Assert.Equal("Inclui filtro", service.Description);
        Assert.Equal(Price, service.BasePrice);
        Assert.Equal(45, service.EstimatedMinutes);
        Assert.True(service.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutDescription_StoresNull(string? description)
    {
        Assert.Null(Service.Create("Alinhamento", description, Price, 30, 10m, 90).Value.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Create_WithTooShortName_ReturnsInvalidName(string name)
    {
        Assert.Equal(CatalogErrors.InvalidName, Service.Create(name, null, Price, 30, 10m, 90).Error);
    }

    [Fact]
    public void Create_WithNameAbove120Characters_ReturnsInvalidName()
    {
        Assert.Equal(CatalogErrors.InvalidName, Service.Create(new string('S', 121), null, Price, 30, 10m, 90).Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithNonPositiveEstimatedMinutes_ReturnsInvalidEstimatedMinutes(int minutes)
    {
        Assert.Equal(CatalogErrors.InvalidEstimatedMinutes, Service.Create("Alinhamento", null, Price, minutes, 10m, 90).Error);
    }

    [Fact]
    public void Create_WithZeroPrice_ReturnsInvalidBasePrice()
    {
        Assert.Equal(CatalogErrors.InvalidBasePrice, Service.Create("Alinhamento", null, Money.Zero, 30, 10m, 90).Error);
    }

    [Fact]
    public void ChangeDetails_WithValidData_UpdatesNameDescriptionAndTime()
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;

        var result = service.ChangeDetails("Alinhamento 3D", "Com balanceamento", 40, 10m, 90);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alinhamento 3D", service.Name);
        Assert.Equal("Com balanceamento", service.Description);
        Assert.Equal(40, service.EstimatedMinutes);
        Assert.Equal(Price, service.BasePrice);
    }

    [Fact]
    public void ChangeDetails_WithInvalidTime_KeepsPreviousData()
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;

        var result = service.ChangeDetails("Alinhamento 3D", null, 0, 10m, 90);

        Assert.Equal(CatalogErrors.InvalidEstimatedMinutes, result.Error);
        Assert.Equal("Alinhamento", service.Name);
        Assert.Equal(30, service.EstimatedMinutes);
    }

    [Fact]
    public void ChangePrice_ReplacesBasePrice()
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;
        var newPrice = Money.Create(180m).Value;

        var result = service.ChangePrice(newPrice);

        Assert.True(result.IsSuccess);
        Assert.Equal(newPrice, service.BasePrice);
    }

    [Fact]
    public void ChangePrice_WithZeroPrice_KeepsPreviousPrice()
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;

        var result = service.ChangePrice(Money.Zero);

        Assert.Equal(CatalogErrors.InvalidBasePrice, result.Error);
        Assert.Equal(Price, service.BasePrice);
    }

    [Fact]
    public void ChangeDetails_InactiveService_ReturnsCannotChangeInactiveAndKeepsData()
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;
        service.Deactivate();

        var result = service.ChangeDetails("Alinhamento 3D", "Com balanceamento", 40, 10m, 90);

        Assert.Equal(CatalogErrors.CannotChangeInactive, result.Error);
        Assert.Equal("Alinhamento", service.Name);
        Assert.Null(service.Description);
        Assert.Equal(30, service.EstimatedMinutes);
    }

    [Fact]
    public void ChangePrice_InactiveService_ReturnsCannotChangeInactiveAndKeepsPrice()
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;
        service.Deactivate();

        var result = service.ChangePrice(Money.Create(180m).Value);

        Assert.Equal(CatalogErrors.CannotChangeInactive, result.Error);
        Assert.Equal(Price, service.BasePrice);
    }

    [Fact]
    public void Deactivate_ActiveService_BecomesInactive()
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;

        var result = service.Deactivate();

        Assert.True(result.IsSuccess);
        Assert.False(service.IsActive);
    }

    [Fact]
    public void Deactivate_InactiveService_ReturnsAlreadyInactive()
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;
        service.Deactivate();

        Assert.Equal(CatalogErrors.AlreadyInactive, service.Deactivate().Error);
    }

    [Fact]
    public void Reactivate_InactiveService_BecomesActive()
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;
        service.Deactivate();

        var result = service.Reactivate();

        Assert.True(result.IsSuccess);
        Assert.True(service.IsActive);
    }

    [Fact]
    public void Reactivate_ActiveService_ReturnsAlreadyActive()
    {
        var service = Service.Create("Alinhamento", null, Price, 30, 10m, 90).Value;

        Assert.Equal(CatalogErrors.AlreadyActive, service.Reactivate().Error);
    }
}
