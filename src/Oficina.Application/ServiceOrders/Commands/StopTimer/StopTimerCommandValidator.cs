using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.StopTimer;

public sealed class StopTimerCommandValidator : AbstractValidator<StopTimerCommand>
{
    public StopTimerCommandValidator() => RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
