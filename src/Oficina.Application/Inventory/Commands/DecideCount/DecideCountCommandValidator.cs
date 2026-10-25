using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Inventory.Commands.DecideCount;

public sealed class DecideCountCommandValidator : AbstractValidator<DecideCountCommand>
{
    public DecideCountCommandValidator()
    {
        RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(request => request.Action).Must(action => action is "submit" or "approve" or "reject").WithError(CommonErrors.InvalidFormat);
    }
}
