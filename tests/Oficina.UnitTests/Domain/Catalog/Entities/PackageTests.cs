using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.UnitTests.Domain.Catalog.Entities;

public sealed class PackageTests
{
    private static readonly Money Price = Money.Create(140m).Value;
    private static readonly Quantity One = Quantity.Create(1m).Value;
    private static readonly Guid ServiceId = Guid.CreateVersion7();
    private static readonly Guid PartId = Guid.CreateVersion7();

    private static IReadOnlyCollection<PackageItemInput> Items() =>
        [new(CatalogItemKind.Service, ServiceId, One), new(CatalogItemKind.Part, PartId, Quantity.Create(2m).Value)];

    [Fact]
    public void Create_WithValidData_IsActiveWithTrimmedFieldsAndItems()
    {
        var result = Package.Create(" Revisão 10 mil ", " Óleo e filtro ", Price, Items(), 10000, 15000);

        Assert.True(result.IsSuccess);
        var package = result.Value;
        Assert.Equal("Revisão 10 mil", package.Name);
        Assert.Equal("Óleo e filtro", package.Description);
        Assert.Equal(Price, package.Price);
        Assert.Equal((10000, 15000), (package.MinMileage, package.MaxMileage));
        Assert.True(package.IsActive);
        Assert.Equal(2, package.Items.Count);
        Assert.True(package.Contains(CatalogItemKind.Part, PartId));
        Assert.False(package.Contains(CatalogItemKind.Service, PartId));
    }

    [Fact]
    public void Create_WithBlankDescription_KeepsNull() =>
        Assert.Null(Package.Create("Revisão", "  ", Price, Items(), null, null).Value.Description);

    [Theory]
    [InlineData("")]
    [InlineData("R")]
    public void Create_WithInvalidName_ReturnsInvalidName(string name) =>
        Assert.Equal(PackageErrors.InvalidName, Package.Create(name, null, Price, Items(), null, null).Error);

    [Fact]
    public void Create_WithLongDescription_ReturnsInvalidDescription() =>
        Assert.Equal(PackageErrors.InvalidDescription, Package.Create("Revisão", new string('d', 501), Price, Items(), null, null).Error);

    [Fact]
    public void Create_WithZeroPrice_ReturnsInvalidPrice() =>
        Assert.Equal(PackageErrors.InvalidPrice, Package.Create("Revisão", null, Money.Zero, Items(), null, null).Error);

    public static TheoryData<PackageItemInput[]> InvalidItems => new()
    {
        Array.Empty<PackageItemInput>(),
        new PackageItemInput[] { new(CatalogItemKind.Service, ServiceId, One), new(CatalogItemKind.Service, ServiceId, One) },
        new PackageItemInput[] { new(CatalogItemKind.Service, Guid.Empty, One) },
        new PackageItemInput[] { new(CatalogItemKind.Package, ServiceId, One) },
        Enumerable.Range(0, Package.MaxItems + 1).Select(_ => new PackageItemInput(CatalogItemKind.Part, Guid.CreateVersion7(), One)).ToArray(),
    };

    [Theory]
    [MemberData(nameof(InvalidItems))]
    public void Create_WithInvalidItems_ReturnsInvalidItems(PackageItemInput[] items) =>
        Assert.Equal(PackageErrors.InvalidItems, Package.Create("Revisão", null, Price, items, null, null).Error);

    [Theory]
    [InlineData(-1, null)]
    [InlineData(null, -1)]
    [InlineData(20000, 10000)]
    public void Create_WithInvalidMileage_ReturnsInvalidMileageRange(int? min, int? max) =>
        Assert.Equal(PackageErrors.InvalidMileageRange, Package.Create("Revisão", null, Price, Items(), min, max).Error);

    [Fact]
    public void Change_ActivePackage_ReplacesEverything()
    {
        var package = Package.Create("Revisão", null, Price, Items(), null, null).Value;

        var result = package.Change("Revisão completa", "Tudo", Money.Create(200m).Value, [new(CatalogItemKind.Part, PartId, One)], 0, null);

        Assert.True(result.IsSuccess);
        Assert.Equal("Revisão completa", package.Name);
        Assert.Equal(200m, package.Price.Amount);
        Assert.Equal(PartId, Assert.Single(package.Items).ItemId);
        Assert.Equal(0, package.MinMileage);
    }

    [Fact]
    public void Change_InactivePackage_ReturnsCannotChangeInactive()
    {
        var package = Package.Create("Revisão", null, Price, Items(), null, null).Value;
        package.Deactivate();

        Assert.Equal(PackageErrors.CannotChangeInactive, package.Change("Outro", null, Price, Items(), null, null).Error);
        Assert.Equal("Revisão", package.Name);
    }

    [Fact]
    public void DeactivateAndReactivate_ToggleTheStatusOnce()
    {
        var package = Package.Create("Revisão", null, Price, Items(), null, null).Value;

        Assert.Equal(PackageErrors.AlreadyActive, package.Reactivate().Error);
        Assert.True(package.Deactivate().IsSuccess);
        Assert.False(package.IsActive);
        Assert.Equal(PackageErrors.AlreadyInactive, package.Deactivate().Error);
        Assert.True(package.Reactivate().IsSuccess);
        Assert.True(package.IsActive);
    }

    [Fact]
    public void UsedByActivePackage_ListsThePackageNames()
    {
        var error = CatalogErrors.UsedByActivePackage(["Revisão 10 mil", "Revisão 20 mil"]);

        Assert.Equal(CatalogErrors.UsedByActivePackageCode, error.Code);
        Assert.Contains("Revisão 10 mil, Revisão 20 mil", error.Message);
    }
}
