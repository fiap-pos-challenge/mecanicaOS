using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Inventory.Errors;

namespace Oficina.Application.Inventory.Commands.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(request => request.Lines).NotEmpty().WithError(InventoryErrors.InvalidOrder);
    }
}
