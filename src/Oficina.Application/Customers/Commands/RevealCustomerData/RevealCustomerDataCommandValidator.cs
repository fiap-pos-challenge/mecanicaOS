using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Customers.Commands.RevealCustomerData;

public sealed class RevealCustomerDataCommandValidator : AbstractValidator<RevealCustomerDataCommand>
{
    public RevealCustomerDataCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Field).IsInEnum().WithError(CommonErrors.ValidationFailed);
        RuleFor(command => command.Reason).IsInEnum().WithError(CommonErrors.ValidationFailed);
    }
}
