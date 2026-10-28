using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.RemoveItem;

public sealed class RemoveItemCommandValidator : AbstractValidator<RemoveItemCommand>
{
    public RemoveItemCommandValidator()
    {
        RuleFor(command => command.ServiceOrderId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.ItemId).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
