using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Inventory.Commands.DeactivatePart;

public sealed class DeactivatePartCommandValidator : AbstractValidator<DeactivatePartCommand>
{
    public DeactivatePartCommandValidator() =>
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
