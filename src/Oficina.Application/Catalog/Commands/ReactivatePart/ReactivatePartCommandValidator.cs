using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Commands.ReactivatePart;

public sealed class ReactivatePartCommandValidator : AbstractValidator<ReactivatePartCommand>
{
    public ReactivatePartCommandValidator() =>
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
