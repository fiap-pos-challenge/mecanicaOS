using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Vehicles.Queries.GetById;

public sealed class GetVehicleByIdValidator : AbstractValidator<GetVehicleByIdQuery>
{
    public GetVehicleByIdValidator() => RuleFor(query => query.Id).NotEmpty()
        .WithError(CommonErrors.RequiredField);
}
