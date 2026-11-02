using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Commands.RejectQuoteByToken;

public sealed class RejectQuoteByTokenCommandValidator : AbstractValidator<RejectQuoteByTokenCommand>
{
    public RejectQuoteByTokenCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty().WithError(ServiceOrderErrors.InvalidApprovalToken);
        RuleFor(command => command.Reason).Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithError(ServiceOrderErrors.RejectionReasonRequired);
    }
}
