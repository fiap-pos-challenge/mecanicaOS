using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Commands.DeactivatePackage;

public sealed class DeactivatePackageCommandValidator : AbstractValidator<DeactivatePackageCommand>
{
    public DeactivatePackageCommandValidator() =>
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
