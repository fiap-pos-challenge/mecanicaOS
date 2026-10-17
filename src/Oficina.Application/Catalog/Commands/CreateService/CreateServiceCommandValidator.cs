using FluentValidation;
using Oficina.Application.Catalog.Validation;

namespace Oficina.Application.Catalog.Commands.CreateService;

public sealed class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(command => command.Name).ValidServiceName();
        RuleFor(command => command.BasePrice).ValidBasePrice();
        RuleFor(command => command.EstimatedMinutes).ValidEstimatedMinutes();
        RuleFor(command => command.CommissionPercent).ValidCommissionPercent();
        RuleFor(command => command.WarrantyDays).ValidWarrantyDays();
    }
}
