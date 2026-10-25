using FluentValidation;
using Oficina.Application.Catalog.Validation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Commands.UpdatePart;

public sealed class UpdatePartCommandValidator : AbstractValidator<UpdatePartCommand>
{
    public UpdatePartCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Code).ValidPartCode();
        RuleFor(command => command.Name).ValidPartName();
        RuleFor(command => command.Type).IsInEnum().WithError(CommonErrors.InvalidFormat);
        RuleFor(command => command.UnitOfMeasure).ValidUnitOfMeasure();
        RuleFor(command => command.UnitPrice).ValidAmount();
        RuleFor(command => command.Cost).ValidAmount();
        RuleFor(command => command.MinimumStock).ValidMinimumStock();
        RuleFor(command => command.Supplier).ValidSupplier();
        RuleFor(command => command.Barcode).ValidBarcode();
    }
}
