using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Inventory.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Inventory.Errors;

namespace Oficina.Application.Inventory.Commands.RegisterStockEntry;

public sealed class RegisterStockEntryCommandValidator : AbstractValidator<RegisterStockEntryCommand>
{
    public RegisterStockEntryCommandValidator()
    {
        RuleFor(command => command.PartId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Quantity).ValidQuantity();
        RuleFor(command => command.Reason).Must(reason => !string.IsNullOrWhiteSpace(reason)).WithError(InventoryErrors.InvalidReason);
    }
}
