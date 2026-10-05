using FluentValidation;
using Oficina.Application.Customers.Validation;

namespace Oficina.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(command => command.Name).ValidCustomerName();
        RuleFor(command => command.Document).ValidDocument();
        RuleFor(command => command.Email).ValidOptionalEmail();
        RuleFor(command => command.Phone).ValidPhone();
        RuleFor(command => command.Address).ValidAddress();
    }
}
