using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Inventory.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Inventory.Commands.UpdatePart;

public sealed class UpdatePartCommandValidator : AbstractValidator<UpdatePartCommand>
{
    public UpdatePartCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Name).ValidPartName();
        RuleFor(command => command.Type).IsInEnum().WithError(CommonErrors.InvalidFormat);
        RuleFor(command => command.UnitOfMeasure).ValidUnitOfMeasure();
        RuleFor(command => command.UnitPrice).ValidUnitPrice();
        RuleFor(command => command.MinimumStock).ValidMinimumStock();
    }
}
