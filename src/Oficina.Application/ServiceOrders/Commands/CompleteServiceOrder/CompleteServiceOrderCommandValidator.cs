using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.CompleteServiceOrder;

public sealed class CompleteServiceOrderCommandValidator : AbstractValidator<CompleteServiceOrderCommand>
{
    public CompleteServiceOrderCommandValidator() => RuleFor(command => command.ServiceOrderId).NotEmpty()
        .WithError(CommonErrors.RequiredField);
}
