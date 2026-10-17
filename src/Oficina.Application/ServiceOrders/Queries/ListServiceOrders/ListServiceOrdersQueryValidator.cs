using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.ServiceOrders.Validation;
using Oficina.Application.Vehicles.Validation;

namespace Oficina.Application.ServiceOrders.Queries.ListServiceOrders;

public sealed class ListServiceOrdersQueryValidator : AbstractValidator<ListServiceOrdersQuery>
{
    public ListServiceOrdersQueryValidator()
    {
        RuleFor(query => query.Page).ValidPage();
        RuleFor(query => query.PageSize).ValidPageSize();
        RuleFor(query => query.Status).ValidOptionalStatus();
        RuleFor(query => query.Plate).ValidOptionalPlate();
    }
}
