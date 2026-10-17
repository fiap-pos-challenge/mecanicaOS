using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Inventory.Commands.RegisterExit;

public sealed class RegisterExitCommandValidator : AbstractValidator<RegisterExitCommand>
{
    public RegisterExitCommandValidator()
    {
        RuleFor(request => request.PartId).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
