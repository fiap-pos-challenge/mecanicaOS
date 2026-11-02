using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Validation;

public static class ServiceOrderRules
{
    public static IRuleBuilderOptionsConditions<T, string?> ValidOptionalStatus<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MustCreate(ToOptionalStatus);

    public static Result<ServiceOrderStatus?> ToOptionalStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<ServiceOrderStatus?>.Success(null);
        }

        if (!Enum.GetNames<ServiceOrderStatus>().Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return ServiceOrderErrors.InvalidStatus;
        }

        return Enum.Parse<ServiceOrderStatus>(value.Trim(), ignoreCase: true);
    }
}
