using FluentValidation;
using Oficina.Application.Vehicles.Validation;

namespace Oficina.Application.Vehicles.Queries.SearchVehicle;

public sealed class SearchVehicleQueryValidator : AbstractValidator<SearchVehicleQuery>
{
    public SearchVehicleQueryValidator() => RuleFor(query => query.LicensePlate).ValidPlate();
}
