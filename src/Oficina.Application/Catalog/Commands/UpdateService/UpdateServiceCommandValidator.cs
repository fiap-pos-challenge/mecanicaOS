using FluentValidation;
using Oficina.Application.Catalog.Validation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Commands.UpdateService;

public sealed class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Name).ValidServiceName();
        RuleFor(command => command.BasePrice).ValidBasePrice();
        RuleFor(command => command.EstimatedMinutes).ValidEstimatedMinutes();
    }
}
