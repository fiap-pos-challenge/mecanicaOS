using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Vehicles.Queries.ListCustomerVehicles;

public sealed class ListCustomerVehiclesQueryValidator : AbstractValidator<ListCustomerVehiclesQuery>
{
    public ListCustomerVehiclesQueryValidator() => RuleFor(query => query.CustomerId).NotEmpty()
        .WithError(CommonErrors.RequiredField);
}
