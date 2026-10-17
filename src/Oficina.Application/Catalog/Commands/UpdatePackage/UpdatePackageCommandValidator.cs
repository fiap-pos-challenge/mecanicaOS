using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Commands.UpdatePackage;

public sealed class UpdatePackageCommandValidator : AbstractValidator<UpdatePackageCommand>
{
    public UpdatePackageCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Name).Must(name => name?.Trim().Length is >= Package.NameMinLength and <= Package.NameMaxLength)
            .WithError(PackageErrors.InvalidName);
        RuleFor(command => command.Description).Must(description => description is null || description.Trim().Length <= Package.DescriptionMaxLength)
            .WithError(PackageErrors.InvalidDescription);
        RuleFor(command => command.Price).GreaterThan(0m).WithError(PackageErrors.InvalidPrice);
        RuleFor(command => command.Items).Must(items => items is { Count: > 0 and <= Package.MaxItems } && items.All(item => item.Quantity > 0))
            .WithError(PackageErrors.InvalidItems);
        RuleFor(command => command.MinMileage).Must((command, min) => min is null or >= 0 && command.MaxMileage is null or >= 0 && !(min > command.MaxMileage))
            .WithError(PackageErrors.InvalidMileageRange);
    }
}
