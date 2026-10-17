using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Application.Catalog.Validation;

public static class ServiceRules
{
    public static IRuleBuilderOptions<T, string> ValidServiceName<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(name => name?.Trim().Length is >= Service.NameMinLength and <= Service.NameMaxLength)
            .WithError(CatalogErrors.InvalidName);

    public static IRuleBuilderOptions<T, decimal> ValidBasePrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.Must(price => Money.Create(price) is { IsSuccess: true } money && money.Value.Amount > 0)
            .WithError(CatalogErrors.InvalidBasePrice);

    public static IRuleBuilderOptions<T, decimal> ValidCommissionPercent<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.InclusiveBetween(0m, Service.MaxCommissionPercent).WithError(CatalogErrors.InvalidCommissionPercent);

    public static IRuleBuilderOptions<T, int> ValidWarrantyDays<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(0, Service.MaxWarrantyDays).WithError(CatalogErrors.InvalidWarrantyDays);

    public static IRuleBuilderOptions<T, int> ValidEstimatedMinutes<T>(this IRuleBuilder<T, int> rule) =>
        rule.GreaterThan(0).WithError(CatalogErrors.InvalidEstimatedMinutes);
}
