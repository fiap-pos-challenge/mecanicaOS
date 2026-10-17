using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.ValueObjects;

namespace Oficina.Application.Catalog.Validation;

public static class PartRules
{
    public static IRuleBuilderOptionsConditions<T, string> ValidPartCode<T>(this IRuleBuilder<T, string> rule) =>
        rule.MustCreate(PartCode.Create);

    public static IRuleBuilderOptions<T, string> ValidPartName<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(name => name?.Trim().Length is >= Part.NameMinLength and <= Part.NameMaxLength)
            .WithError(InventoryErrors.InvalidName);

    public static IRuleBuilderOptions<T, string> ValidUnitOfMeasure<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(unit => unit?.Trim().Length is >= 1 and <= Part.UnitOfMeasureMaxLength)
            .WithError(InventoryErrors.InvalidUnitOfMeasure);

    public static IRuleBuilderOptions<T, decimal> ValidAmount<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThanOrEqualTo(0m).WithError(CommonErrors.NegativeAmount);

    public static IRuleBuilderOptions<T, decimal> ValidMinimumStock<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.Must(value => value >= 0 && value * 1000m == decimal.Truncate(value * 1000m))
            .WithError(InventoryErrors.InvalidMinimumStock);

    public static IRuleBuilderOptions<T, string?> ValidSupplier<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(supplier => supplier is null || supplier.Trim().Length <= Part.SupplierMaxLength)
            .WithError(InventoryErrors.InvalidSupplier);

    public static IRuleBuilderOptions<T, string?> ValidBarcode<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(barcode => string.IsNullOrWhiteSpace(barcode) || Part.IsValidBarcode(barcode.Trim()))
            .WithError(InventoryErrors.InvalidBarcode);
}
