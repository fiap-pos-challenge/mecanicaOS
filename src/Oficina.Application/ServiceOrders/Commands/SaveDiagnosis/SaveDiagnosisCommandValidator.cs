using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.SaveDiagnosis;

public sealed class SaveDiagnosisCommandValidator : AbstractValidator<SaveDiagnosisCommand>
{
    public SaveDiagnosisCommandValidator() => RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
