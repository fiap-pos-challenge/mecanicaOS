using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.FinishService;

public sealed class FinishServiceCommandValidator : AbstractValidator<FinishServiceCommand>
{
    public FinishServiceCommandValidator() => RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
