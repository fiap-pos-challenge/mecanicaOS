using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.DeliverServiceOrder;

public sealed class DeliverServiceOrderCommandValidator : AbstractValidator<DeliverServiceOrderCommand>
{
    public DeliverServiceOrderCommandValidator() => RuleFor(command => command.ServiceOrderId).NotEmpty()
        .WithError(CommonErrors.RequiredField);
}
