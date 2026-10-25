using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Domain.Catalog.Entities;

public sealed class Service : AggregateRoot
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 120;
    public const decimal MaxCommissionPercent = 50m;
    public const int MaxWarrantyDays = 3650;

    private readonly List<ServiceUsualPart> _usualParts;

    private Service(Guid id, string name, string? description, Money basePrice, int estimatedMinutes, decimal commissionPercent, int warrantyDays, bool isActive)
        : base(id)
    {
        Name = name;
        Description = description;
        BasePrice = basePrice;
        EstimatedMinutes = estimatedMinutes;
        CommissionPercent = commissionPercent;
        WarrantyDays = warrantyDays;
        IsActive = isActive;
        _usualParts = new List<ServiceUsualPart>();
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public Money BasePrice { get; private set; }

    public int EstimatedMinutes { get; private set; }

    public decimal CommissionPercent { get; private set; }

    public int WarrantyDays { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<ServiceUsualPart> UsualParts => _usualParts.AsReadOnly();

    public static Result<Service> Create(string name, string? description, Money basePrice, int estimatedMinutes, decimal commissionPercent, int warrantyDays)
    {
        if ((FindDetailsError(name, estimatedMinutes) ?? FindTermsError(commissionPercent, warrantyDays) ?? FindPriceError(basePrice)) is { } error)
        {
            return error;
        }

        return new Service(Guid.CreateVersion7(), name.Trim(), NormalizeDescription(description), basePrice, estimatedMinutes, commissionPercent, warrantyDays, isActive: true);
    }

    public Result ChangeDetails(string name, string? description, int estimatedMinutes, decimal commissionPercent, int warrantyDays)
    {
        if ((FindInactiveError() ?? FindDetailsError(name, estimatedMinutes) ?? FindTermsError(commissionPercent, warrantyDays)) is { } error)
        {
            return error;
        }

        Name = name.Trim();
        Description = NormalizeDescription(description);
        EstimatedMinutes = estimatedMinutes;
        CommissionPercent = commissionPercent;
        WarrantyDays = warrantyDays;
        return Result.Success();
    }

    public Result ChangePrice(Money basePrice)
    {
        if ((FindInactiveError() ?? FindPriceError(basePrice)) is { } error)
        {
            return error;
        }

        BasePrice = basePrice;
        return Result.Success();
    }

    public Result ReplaceUsualParts(IReadOnlyCollection<(Guid PartId, Quantity Quantity)> parts)
    {
        if (FindInactiveError() is { } inactive)
        {
            return inactive;
        }

        if (parts.Any(part => part.PartId == Guid.Empty) || parts.Select(part => part.PartId).Distinct().Count() != parts.Count)
        {
            return CatalogErrors.InvalidUsualParts;
        }

        _usualParts.Clear();
        _usualParts.AddRange(parts.Select(part => ServiceUsualPart.Create(part.PartId, part.Quantity)));
        return Result.Success();
    }

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

    private static Error? FindTermsError(decimal commissionPercent, int warrantyDays)
    {
        if (commissionPercent is < 0 or > MaxCommissionPercent)
        {
            return CatalogErrors.InvalidCommissionPercent;
        }

        if (warrantyDays is < 0 or > MaxWarrantyDays)
        {
            return CatalogErrors.InvalidWarrantyDays;
        }

        return null;
    }

    private Error? FindInactiveError() => IsActive ? null : CatalogErrors.CannotChangeInactive;

    private static Error? FindPriceError(Money basePrice) =>
        basePrice.Amount <= 0 ? CatalogErrors.InvalidBasePrice : null;

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
