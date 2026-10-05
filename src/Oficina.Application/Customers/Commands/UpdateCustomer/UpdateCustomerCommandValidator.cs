using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Customers.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Customers.Commands.UpdateCustomer;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Name).ValidCustomerName();
        RuleFor(command => command.Email).ValidOptionalEmail();
        RuleFor(command => command.Phone).ValidPhone();
        RuleFor(command => command.Address).ValidAddress();
    }
}
