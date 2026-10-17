using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.StartTimer;

public sealed class StartTimerCommandValidator : AbstractValidator<StartTimerCommand>
{
    public StartTimerCommandValidator() => RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
