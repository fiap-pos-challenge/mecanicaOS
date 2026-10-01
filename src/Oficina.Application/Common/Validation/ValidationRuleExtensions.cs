using FluentValidation;
using FluentValidation.Results;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Common.Validation;

public static class ValidationRuleExtensions
{
    public static IRuleBuilderOptions<T, TProperty> WithError<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Error error) =>
        rule.WithErrorCode(error.Code).WithMessage(error.Message);

    public static IRuleBuilderOptionsConditions<T, TProperty> MustCreate<T, TProperty, TValue>(
        this IRuleBuilder<T, TProperty> rule,
        Func<TProperty, Result<TValue>> factory) =>
        rule.Custom((value, context) =>
        {
            if (factory(value).Error is { } error)
            {
                context.AddFailure(new ValidationFailure(context.PropertyPath, error.Message) { ErrorCode = error.Code });
            }
        });

    public static IRuleBuilderOptions<T, int> ValidPage<T>(this IRuleBuilder<T, int> rule) =>
        rule.GreaterThanOrEqualTo(PageRequest.FirstPage).WithError(CommonErrors.InvalidPage);

    public static IRuleBuilderOptions<T, int> ValidPageSize<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, PageRequest.MaxPageSize).WithError(CommonErrors.InvalidPageSize);
}
