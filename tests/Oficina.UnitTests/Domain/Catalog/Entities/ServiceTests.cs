using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.UnitTests.Domain.Catalog.Entities;

public sealed class ServiceTests
{
    private static readonly Money Price = Money.Create(150m).Value;

    [Fact]
    public void Create_WithValidData_IsActiveWithCopiedFields()
    {
        var result = Service.Create(" Troca de óleo ", " Inclui filtro ", Price, 45);

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
        Assert.Null(Service.Create("Alinhamento", description, Price, 30).Value.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Create_WithTooShortName_ReturnsInvalidName(string name)
    {
        Assert.Equal(CatalogErrors.InvalidName, Service.Create(name, null, Price, 30).Error);
    }

    [Fact]
    public void Create_WithNameAbove120Characters_ReturnsInvalidName()
    {
        Assert.Equal(CatalogErrors.InvalidName, Service.Create(new string('S', 121), null, Price, 30).Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithNonPositiveEstimatedMinutes_ReturnsInvalidEstimatedMinutes(int minutes)
    {
        Assert.Equal(CatalogErrors.InvalidEstimatedMinutes, Service.Create("Alinhamento", null, Price, minutes).Error);
    }

    [Fact]
    public void ChangeDetails_WithValidData_UpdatesNameDescriptionAndTime()
    {
        var service = Service.Create("Alinhamento", null, Price, 30).Value;

        var result = service.ChangeDetails("Alinhamento 3D", "Com balanceamento", 40);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alinhamento 3D", service.Name);
        Assert.Equal("Com balanceamento", service.Description);
        Assert.Equal(40, service.EstimatedMinutes);
        Assert.Equal(Price, service.BasePrice);
    }

    [Fact]
    public void ChangeDetails_WithInvalidTime_KeepsPreviousData()
    {
        var service = Service.Create("Alinhamento", null, Price, 30).Value;

        var result = service.ChangeDetails("Alinhamento 3D", null, 0);

        Assert.Equal(CatalogErrors.InvalidEstimatedMinutes, result.Error);
        Assert.Equal("Alinhamento", service.Name);
        Assert.Equal(30, service.EstimatedMinutes);
    }

    [Fact]
    public void ChangePrice_ReplacesBasePrice()
    {
        var service = Service.Create("Alinhamento", null, Price, 30).Value;
        var newPrice = Money.Create(180m).Value;

        service.ChangePrice(newPrice);

        Assert.Equal(newPrice, service.BasePrice);
    }

    [Fact]
    public void Deactivate_ActiveService_BecomesInactive()
    {
        var service = Service.Create("Alinhamento", null, Price, 30).Value;

        var result = service.Deactivate();

        Assert.True(result.IsSuccess);
        Assert.False(service.IsActive);
    }

    [Fact]
    public void Deactivate_InactiveService_ReturnsAlreadyInactive()
    {
        var service = Service.Create("Alinhamento", null, Price, 30).Value;
        service.Deactivate();

        Assert.Equal(CatalogErrors.AlreadyInactive, service.Deactivate().Error);
    }

    [Fact]
    public void Reactivate_InactiveService_BecomesActive()
    {
        var service = Service.Create("Alinhamento", null, Price, 30).Value;
        service.Deactivate();

        var result = service.Reactivate();

        Assert.True(result.IsSuccess);
        Assert.True(service.IsActive);
    }

    [Fact]
    public void Reactivate_ActiveService_ReturnsAlreadyActive()
    {
        var service = Service.Create("Alinhamento", null, Price, 30).Value;

        Assert.Equal(CatalogErrors.AlreadyActive, service.Reactivate().Error);
    }
}
