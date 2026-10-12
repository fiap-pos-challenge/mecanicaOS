using FluentValidation;
using Oficina.Application.Vehicles.Validation;

namespace Oficina.Application.Vehicles.Commands.UpdateVehicle;

public sealed class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(command => command.Brand).ValidBrand();
        RuleFor(command => command.Model).ValidModel();
        RuleFor(command => command.Year).ValidYear();
    }
}
