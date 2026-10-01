using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Domain.Catalog.Entities;

public sealed class Service : AggregateRoot
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 120;

    private Service(Guid id, string name, string? description, Money basePrice, int estimatedMinutes, bool isActive)
        : base(id)
    {
        Name = name;
        Description = description;
        BasePrice = basePrice;
        EstimatedMinutes = estimatedMinutes;
        IsActive = isActive;
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public Money BasePrice { get; private set; }

    public int EstimatedMinutes { get; private set; }

    public bool IsActive { get; private set; }

    public static Result<Service> Create(string name, string? description, Money basePrice, int estimatedMinutes)
    {
        if (FindDetailsError(name, estimatedMinutes) is { } error)
        {
            return error;
        }

        return new Service(Guid.CreateVersion7(), name.Trim(), NormalizeDescription(description), basePrice, estimatedMinutes, isActive: true);
    }

    public Result ChangeDetails(string name, string? description, int estimatedMinutes)
    {
        if (FindDetailsError(name, estimatedMinutes) is { } error)
        {
            return error;
        }

        Name = name.Trim();
        Description = NormalizeDescription(description);
        EstimatedMinutes = estimatedMinutes;
        return Result.Success();
    }

    public void ChangePrice(Money basePrice) => BasePrice = basePrice;

    public Result Deactivate()
    {
        if (!IsActive)
        {
            return CatalogErrors.AlreadyInactive;
        }

        IsActive = false;
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (IsActive)
        {
            return CatalogErrors.AlreadyActive;
        }

        IsActive = true;
        return Result.Success();
    }

    private static Error? FindDetailsError(string name, int estimatedMinutes)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length is < NameMinLength or > NameMaxLength)
        {
            return CatalogErrors.InvalidName;
        }

        if (estimatedMinutes <= 0)
        {
            return CatalogErrors.InvalidEstimatedMinutes;
        }

        return null;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
