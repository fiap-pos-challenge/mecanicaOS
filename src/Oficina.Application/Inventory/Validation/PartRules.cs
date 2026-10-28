using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.ValueObjects;

namespace Oficina.Application.Inventory.Validation;

public static class PartRules
{
    public static IRuleBuilderOptionsConditions<T, string> ValidPartCode<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithError(InventoryErrors.InvalidPartCode).MustCreate(PartCode.Create);

    public static IRuleBuilderOptions<T, string> ValidPartName<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(name => name?.Trim().Length is >= Part.NameMinLength and <= Part.NameMaxLength)
            .WithError(InventoryErrors.InvalidName);

    public static IRuleBuilderOptions<T, string> ValidUnitOfMeasure<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(unit => unit?.Trim().Length is > 0 and <= Part.UnitOfMeasureMaxLength)
            .WithError(InventoryErrors.InvalidUnitOfMeasure);

    public static IRuleBuilderOptionsConditions<T, decimal> ValidUnitPrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.MustCreate(Money.Create);

    public static IRuleBuilderOptions<T, decimal> ValidMinimumStock<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.Must(minimum => minimum >= 0 && decimal.Truncate(minimum * 1000m) == minimum * 1000m)
            .WithError(InventoryErrors.InvalidMinimumStock);

    public static IRuleBuilderOptionsConditions<T, decimal> ValidQuantity<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.MustCreate(Quantity.Create);
}
