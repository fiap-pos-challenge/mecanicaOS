using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.ApproveQuote;

public sealed class ApproveQuoteCommandValidator : AbstractValidator<ApproveQuoteCommand>
{
    public ApproveQuoteCommandValidator() => RuleFor(command => command.ServiceOrderId).NotEmpty()
        .WithError(CommonErrors.RequiredField);
}
