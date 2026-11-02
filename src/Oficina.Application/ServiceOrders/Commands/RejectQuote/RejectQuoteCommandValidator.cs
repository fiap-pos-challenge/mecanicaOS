using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Commands.RejectQuote;

public sealed class RejectQuoteCommandValidator : AbstractValidator<RejectQuoteCommand>
{
    public RejectQuoteCommandValidator()
    {
        RuleFor(command => command.ServiceOrderId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Reason).Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithError(ServiceOrderErrors.RejectionReasonRequired);
    }
}
