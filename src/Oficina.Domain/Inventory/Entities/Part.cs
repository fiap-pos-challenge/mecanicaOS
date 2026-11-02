using System.Globalization;
using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Enums;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.ValueObjects;

namespace Oficina.Domain.Inventory.Entities;

public sealed class Part : AggregateRoot
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 120;
    public const int UnitOfMeasureMaxLength = 10;
    private const decimal QuantityPrecision = 1000m;
    private const string ServiceOrderReasonPrefix = "serviceOrder:";

    private readonly List<StockMovement> _stockMovements;

    private Part(
        Guid id,
        PartCode code,
        string name,
        PartType type,
        string unitOfMeasure,
        Money unitPrice,
        decimal stockQuantity,
        decimal minimumStock,
        bool isActive)
        : base(id)
    {
        Code = code;
        Name = name;
        Type = type;
        UnitOfMeasure = unitOfMeasure;
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
        MinimumStock = minimumStock;
        IsActive = isActive;
        _stockMovements = new List<StockMovement>();
    }

    public PartCode Code { get; private set; }

    public string Name { get; private set; }

    public PartType Type { get; private set; }

    public string UnitOfMeasure { get; private set; }

    public Money UnitPrice { get; private set; }

    public decimal StockQuantity { get; private set; }

    public decimal MinimumStock { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsBelowMinimum => StockQuantity < MinimumStock;

    public IReadOnlyCollection<StockMovement> StockMovements => _stockMovements.AsReadOnly();

    public static Result<Part> Create(
        PartCode code,
        string name,
        PartType type,
        string unitOfMeasure,
        Money unitPrice,
        decimal minimumStock)
    {
        if (FindDetailsError(name, unitOfMeasure, minimumStock) is { } error)
        {
            return error;
        }

        return new Part(Guid.CreateVersion7(), code, name.Trim(), type, unitOfMeasure.Trim(), unitPrice, 0m, minimumStock, isActive: true);
    }

    public Result ChangeDetails(string name, PartType type, string unitOfMeasure, Money unitPrice, decimal minimumStock)
    {
        if (FindDetailsError(name, unitOfMeasure, minimumStock) is { } error)
        {
            return error;
        }

        Name = name.Trim();
        Type = type;
        UnitOfMeasure = unitOfMeasure.Trim();
        UnitPrice = unitPrice;
        MinimumStock = minimumStock;
        return Result.Success();
    }

    public Result RegisterEntry(Quantity quantity, string reason, Guid? userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return InventoryErrors.InvalidReason;
        }

        StockQuantity += quantity.Value;
        _stockMovements.Add(StockMovement.Record(Id, StockMovementType.Entry, quantity, StockQuantity, reason.Trim(), null, userId));
        return Result.Success();
    }

    public bool HasAvailable(Quantity quantity) => StockQuantity >= quantity.Value;

    public Result DeductForServiceOrder(Quantity quantity, Guid serviceOrderId, long serviceOrderNumber, Guid? userId)
    {
        if (!HasAvailable(quantity))
        {
            return InventoryErrors.InsufficientStock;
        }

        StockQuantity -= quantity.Value;
        var reason = ServiceOrderReasonPrefix + serviceOrderNumber.ToString(CultureInfo.InvariantCulture);
        _stockMovements.Add(StockMovement.Record(Id, StockMovementType.Deduction, quantity, StockQuantity, reason, serviceOrderId, userId));
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
        {
            return InventoryErrors.AlreadyInactive;
        }

        IsActive = false;
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (IsActive)
        {
            return InventoryErrors.AlreadyActive;
        }

        IsActive = true;
        return Result.Success();
    }

    private static Error? FindDetailsError(string name, string unitOfMeasure, decimal minimumStock)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length is < NameMinLength or > NameMaxLength)
        {
            return InventoryErrors.InvalidName;
        }

        if (string.IsNullOrWhiteSpace(unitOfMeasure) || unitOfMeasure.Trim().Length > UnitOfMeasureMaxLength)
        {
            return InventoryErrors.InvalidUnitOfMeasure;
        }

        var scaledMinimum = minimumStock * QuantityPrecision;
        if (minimumStock < 0 || scaledMinimum != decimal.Truncate(scaledMinimum))
        {
            return InventoryErrors.InvalidMinimumStock;
        }

        return null;
    }
}
