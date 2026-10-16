using FluentValidation;
using Oficina.Application.Vehicles.Validation;

namespace Oficina.Application.Vehicles.Commands.CreateVehicle;

public sealed class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{

    public CreateVehicleCommandValidator()
    {
        RuleFor(command => command.Plate).ValidPlate();
        RuleFor(command => command.Year).ValidYear();
        RuleFor(command => command.Model).ValidModel();
        RuleFor(command => command.Brand).ValidBrand();
    }

}
