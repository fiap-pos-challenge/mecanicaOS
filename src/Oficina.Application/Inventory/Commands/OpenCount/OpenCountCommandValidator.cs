using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Inventory.Errors;

namespace Oficina.Application.Inventory.Commands.OpenCount;

public sealed class OpenCountCommandValidator : AbstractValidator<OpenCountCommand>
{
    public OpenCountCommandValidator()
    {
        RuleFor(request => request.Location).NotEmpty().WithError(InventoryErrors.UnknownLocation);
    }
}
