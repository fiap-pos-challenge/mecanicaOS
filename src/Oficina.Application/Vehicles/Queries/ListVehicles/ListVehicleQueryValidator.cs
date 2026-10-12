using FluentValidation;
using Oficina.Application.Common.Validation;

namespace Oficina.Application.Vehicles.Queries.ListVehicles;

public class ListVehicleQueryValidator : AbstractValidator<ListVehiclesQuery>
{
    public ListVehicleQueryValidator()
    {
        RuleFor(query => query.Page).ValidPage();
        RuleFor(query => query.PageSize).ValidPageSize();
    }
}
