using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.UpdateChecklist;

public sealed class UpdateChecklistCommandValidator : AbstractValidator<UpdateChecklistCommand>
{
    public UpdateChecklistCommandValidator() => RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
