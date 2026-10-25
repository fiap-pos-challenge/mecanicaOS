using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Commands.OpenServiceOrder;

public sealed class OpenServiceOrderCommandValidator : AbstractValidator<OpenServiceOrderCommand>
{
    public OpenServiceOrderCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.VehicleId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.ProblemDescription)
            .Must(description => description?.Trim().Length >= ServiceOrder.ProblemDescriptionMinLength)
            .WithError(ServiceOrderErrors.InvalidProblemDescription);
        RuleFor(command => command.Mileage).Must(mileage => mileage is null or >= 0).WithError(CommonErrors.InvalidFormat);
    }
}
