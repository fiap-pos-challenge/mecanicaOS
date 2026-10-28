using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Inventory.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Inventory.Commands.CreatePart;

public sealed class CreatePartCommandValidator : AbstractValidator<CreatePartCommand>
{
    public CreatePartCommandValidator()
    {
        RuleFor(command => command.Code).ValidPartCode();
        RuleFor(command => command.Name).ValidPartName();
        RuleFor(command => command.Type).IsInEnum().WithError(CommonErrors.InvalidFormat);
        RuleFor(command => command.UnitOfMeasure).ValidUnitOfMeasure();
        RuleFor(command => command.UnitPrice).ValidUnitPrice();
        RuleFor(command => command.MinimumStock).ValidMinimumStock();
    }
}
