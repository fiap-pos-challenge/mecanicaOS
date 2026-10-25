using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Inventory.Errors;

namespace Oficina.Application.Inventory.Commands.RegisterEntry;

public sealed class RegisterEntryCommandValidator : AbstractValidator<RegisterEntryCommand>
{
    public RegisterEntryCommandValidator()
    {
        RuleFor(request => request.PartId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(request => request.UnitCost).GreaterThanOrEqualTo(0m).WithError(InventoryErrors.InvalidUnitCost);
    }
}
