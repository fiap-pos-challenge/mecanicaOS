using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Commands.TransitionServiceOrder;

public sealed class TransitionServiceOrderCommandValidator : AbstractValidator<TransitionServiceOrderCommand>
{
    public TransitionServiceOrderCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.To).IsInEnum().WithError(ServiceOrderErrors.InvalidStatus);
        RuleFor(command => command.Version).GreaterThanOrEqualTo(0).WithError(CommonErrors.InvalidFormat);
    }
}
