using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Vehicles.Commands.TransferVehicle;

public sealed class TransferVehicleCommandValidator : AbstractValidator<TransferVehicleCommand>
{
    public TransferVehicleCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.NewCustomerId).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
