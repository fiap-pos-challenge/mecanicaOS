using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Inventory.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.AddPartItem;

public sealed class AddPartItemCommandValidator : AbstractValidator<AddPartItemCommand>
{
    public AddPartItemCommandValidator()
    {
        RuleFor(command => command.ServiceOrderId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.PartId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Quantity).ValidQuantity();
    }
}
