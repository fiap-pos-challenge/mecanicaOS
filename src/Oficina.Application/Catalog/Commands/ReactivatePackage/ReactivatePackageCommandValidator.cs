using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Commands.ReactivatePackage;

public sealed class ReactivatePackageCommandValidator : AbstractValidator<ReactivatePackageCommand>
{
    public ReactivatePackageCommandValidator() =>
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
