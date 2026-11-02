using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Vehicles.Queries.GetVehicleById;

public sealed class GetVehicleByIdQueryValidator : AbstractValidator<GetVehicleByIdQuery>
{
    public GetVehicleByIdQueryValidator() => RuleFor(query => query.Id).NotEmpty()
        .WithError(CommonErrors.RequiredField);
}
