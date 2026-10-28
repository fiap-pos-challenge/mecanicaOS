using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Commands.AddServiceItem;

public sealed class AddServiceItemCommandValidator : AbstractValidator<AddServiceItemCommand>
{
    public AddServiceItemCommandValidator()
    {
        RuleFor(command => command.ServiceOrderId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.ServiceId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Quantity).GreaterThan(0).WithError(ServiceOrderErrors.InvalidServiceQuantity);
    }
}
