using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Customers.Dtos;
using Oficina.Application.Customers.Mappers;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Customers.Validation;

public static class CustomerRules
{
    public static IRuleBuilderOptions<T, string> ValidCustomerName<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(name => name?.Trim().Length is >= Customer.NameMinLength and <= Customer.NameMaxLength)
            .WithError(CustomerErrors.InvalidName);

    public static IRuleBuilderOptionsConditions<T, string> ValidDocument<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithError(CustomerErrors.InvalidDocument).MustCreate(Document.Create);

    public static IRuleBuilderOptionsConditions<T, string> ValidPhone<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithError(CustomerErrors.InvalidPhone).MustCreate(Phone.Create);

    public static IRuleBuilderOptionsConditions<T, string?> ValidOptionalEmail<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MustCreate(CustomerMapper.ToOptionalEmail);

    public static IRuleBuilderOptionsConditions<T, AddressInput?> ValidAddress<T>(this IRuleBuilder<T, AddressInput?> rule) =>
        rule.MustCreate(CustomerMapper.ToAddress);
}
