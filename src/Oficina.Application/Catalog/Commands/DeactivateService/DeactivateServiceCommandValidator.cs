using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Commands.DeactivateService;

public sealed class DeactivateServiceCommandValidator : AbstractValidator<DeactivateServiceCommand>
{
    public DeactivateServiceCommandValidator() =>
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
