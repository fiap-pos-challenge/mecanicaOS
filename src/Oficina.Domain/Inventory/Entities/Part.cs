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
    public const int SupplierMaxLength = 120;
    public const int BarcodeLength = 13;
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
        Money cost,
        decimal stockQuantity,
        decimal minimumStock,
        string? supplier,
        string? barcode,
        bool isActive)
        : base(id)
    {
        Code = code;
        Name = name;
        Type = type;
        UnitOfMeasure = unitOfMeasure;
        UnitPrice = unitPrice;
        Cost = cost;
        StockQuantity = stockQuantity;
        MinimumStock = minimumStock;
        Supplier = supplier;
        Barcode = barcode;
        IsActive = isActive;
        _stockMovements = new List<StockMovement>();
    }

    public PartCode Code { get; private set; }

    public string Name { get; private set; }

    public PartType Type { get; private set; }

    public string UnitOfMeasure { get; private set; }

    public Money UnitPrice { get; private set; }

    public Money Cost { get; private set; }

    public string? Supplier { get; private set; }

    public string? Barcode { get; private set; }

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
        Money cost,
        decimal minimumStock,
        string? supplier,
        string? barcode)
    {
        if (FindDetailsError(name, unitOfMeasure, minimumStock, supplier, barcode) is { } error)
        {
            return error;
        }

        return new Part(
            Guid.CreateVersion7(),
            code,
            name.Trim(),
            type,
            unitOfMeasure.Trim(),
            unitPrice,
            cost,
            0m,
            minimumStock,
            NormalizeText(supplier),
            NormalizeText(barcode),
            isActive: true);
    }

    public Result ChangeDetails(
        PartCode code,
        string name,
        PartType type,
        string unitOfMeasure,
        Money unitPrice,
        Money cost,
        decimal minimumStock,
        string? supplier,
        string? barcode)
    {
        if (FindDetailsError(name, unitOfMeasure, minimumStock, supplier, barcode) is { } error)
        {
            return error;
        }

        Code = code;
        Name = name.Trim();
        Type = type;
        UnitOfMeasure = unitOfMeasure.Trim();
        UnitPrice = unitPrice;
        Cost = cost;
        MinimumStock = minimumStock;
        Supplier = NormalizeText(supplier);
        Barcode = NormalizeText(barcode);
        return Result.Success();
    }

    public static bool IsValidBarcode(string barcode)
    {
        if (barcode.Length != BarcodeLength || !barcode.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        for (var index = 0; index < BarcodeLength - 1; index++)
        {
            sum += (barcode[index] - '0') * (index % 2 == 0 ? 1 : 3);
        }

        return (10 - (sum % 10)) % 10 == barcode[BarcodeLength - 1] - '0';
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

    private static Error? FindDetailsError(string name, string unitOfMeasure, decimal minimumStock, string? supplier, string? barcode)
    {
        if (NormalizeText(supplier) is { Length: > SupplierMaxLength })
        {
            return InventoryErrors.InvalidSupplier;
        }

        if (NormalizeText(barcode) is { } code && !IsValidBarcode(code))
        {
            return InventoryErrors.InvalidBarcode;
        }

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

    private static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
