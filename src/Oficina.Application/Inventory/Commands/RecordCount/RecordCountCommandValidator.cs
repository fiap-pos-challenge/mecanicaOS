using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Inventory.Commands.RecordCount;

public sealed class RecordCountCommandValidator : AbstractValidator<RecordCountCommand>
{
    public RecordCountCommandValidator()
    {
        RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
