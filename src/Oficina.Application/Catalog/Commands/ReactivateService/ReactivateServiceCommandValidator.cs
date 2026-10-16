using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Commands.ReactivateService;

public sealed class ReactivateServiceCommandValidator : AbstractValidator<ReactivateServiceCommand>
{
    public ReactivateServiceCommandValidator() =>
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
