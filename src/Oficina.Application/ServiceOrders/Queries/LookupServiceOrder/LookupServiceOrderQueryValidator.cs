using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Customers.Validation;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.Application.ServiceOrders.Queries.LookupServiceOrder;

public sealed class LookupServiceOrderQueryValidator : AbstractValidator<LookupServiceOrderQuery>
{
    public LookupServiceOrderQueryValidator()
    {
        RuleFor(query => query.Number).MustCreate(ServiceOrderNumber.Create);
        RuleFor(query => query.Document).ValidDocument();
    }
}
