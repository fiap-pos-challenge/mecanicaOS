using FluentValidation;
using Oficina.Application.Common.Validation;

namespace Oficina.Application.Vehicles.Queries.ListVehicles;

public sealed class ListVehiclesQueryValidator : AbstractValidator<ListVehiclesQuery>
{
    public ListVehiclesQueryValidator()
    {
        RuleFor(query => query.Page).ValidPage();
        RuleFor(query => query.PageSize).ValidPageSize();
    }
}
