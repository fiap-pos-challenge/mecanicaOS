using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.StartDiagnosis;

public sealed class StartDiagnosisCommandValidator : AbstractValidator<StartDiagnosisCommand>
{
    public StartDiagnosisCommandValidator() => RuleFor(command => command.ServiceOrderId).NotEmpty()
        .WithError(CommonErrors.RequiredField);
}
