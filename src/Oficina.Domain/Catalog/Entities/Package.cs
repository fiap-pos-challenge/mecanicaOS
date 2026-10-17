using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Domain.Catalog.Entities;

public sealed class Package : AggregateRoot
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 120;
    public const int DescriptionMaxLength = 500;
    public const int MaxItems = 50;

    private readonly List<PackageItem> _items;

    private Package(Guid id, string name, string? description, Money price, int? minMileage, int? maxMileage, bool isActive)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        MinMileage = minMileage;
        MaxMileage = maxMileage;
        IsActive = isActive;
        _items = new List<PackageItem>();
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public Money Price { get; private set; }

    public int? MinMileage { get; private set; }

    public int? MaxMileage { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<PackageItem> Items => _items.AsReadOnly();

    public static Result<Package> Create(string name, string? description, Money price, IReadOnlyCollection<PackageItemInput> items, int? minMileage, int? maxMileage)
    {
        if (FindError(name, description, price, items, minMileage, maxMileage) is { } error)
        {
            return error;
        }

        var package = new Package(Guid.CreateVersion7(), name.Trim(), NormalizeDescription(description), price, minMileage, maxMileage, isActive: true);
        package.SetItems(items);
        return package;
    }

    public Result Change(string name, string? description, Money price, IReadOnlyCollection<PackageItemInput> items, int? minMileage, int? maxMileage)
    {
        if ((FindInactiveError() ?? FindError(name, description, price, items, minMileage, maxMileage)) is { } error)
        {
            return error;
        }

        Name = name.Trim();
        Description = NormalizeDescription(description);
        Price = price;
        MinMileage = minMileage;
        MaxMileage = maxMileage;
        SetItems(items);
        return Result.Success();
    }

    public bool Contains(CatalogItemKind kind, Guid itemId) =>
        _items.Exists(item => item.Kind == kind && item.ItemId == itemId);

    public Result Deactivate()
    {
        if (!IsActive)
        {
            return PackageErrors.AlreadyInactive;
        }

        IsActive = false;
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (IsActive)
        {
            return PackageErrors.AlreadyActive;
        }

        IsActive = true;
        return Result.Success();
    }

    private void SetItems(IReadOnlyCollection<PackageItemInput> items)
    {
        _items.Clear();
        _items.AddRange(items.Select(item => PackageItem.Create(item.Kind, item.ItemId, item.Quantity)));
    }

    private Error? FindInactiveError() => IsActive ? null : PackageErrors.CannotChangeInactive;

    private static Error? FindError(string name, string? description, Money price, IReadOnlyCollection<PackageItemInput> items, int? minMileage, int? maxMileage)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length is < NameMinLength or > NameMaxLength)
        {
            return PackageErrors.InvalidName;
        }

        if (NormalizeDescription(description) is { Length: > DescriptionMaxLength })
        {
            return PackageErrors.InvalidDescription;
        }

        if (price.Amount <= 0)
        {
            return PackageErrors.InvalidPrice;
        }

        if (items.Count is 0 or > MaxItems
            || items.Any(item => item.ItemId == Guid.Empty || item.Kind == CatalogItemKind.Package)
            || items.Select(item => (item.Kind, item.ItemId)).Distinct().Count() != items.Count)
        {
            return PackageErrors.InvalidItems;
        }

        if (minMileage < 0 || maxMileage < 0 || minMileage > maxMileage)
        {
            return PackageErrors.InvalidMileageRange;
        }

        return null;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
