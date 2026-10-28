using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Customers.Validation;
using Oficina.Application.Vehicles.Validation;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Commands.OpenServiceOrder;

public sealed class OpenServiceOrderCommandValidator : AbstractValidator<OpenServiceOrderCommand>
{
    public OpenServiceOrderCommandValidator()
    {
        RuleFor(command => command.CustomerDocument).ValidDocument();
        RuleFor(command => command.LicensePlate).ValidPlate();
        RuleFor(command => command.ProblemDescription)
            .Must(description => description?.Trim().Length >= ServiceOrder.ProblemDescriptionMinLength)
            .WithError(ServiceOrderErrors.InvalidProblemDescription);
    }
}
