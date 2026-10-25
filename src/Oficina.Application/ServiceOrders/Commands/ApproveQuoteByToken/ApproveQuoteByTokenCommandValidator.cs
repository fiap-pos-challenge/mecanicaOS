using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Commands.ApproveQuoteByToken;

public sealed class ApproveQuoteByTokenCommandValidator : AbstractValidator<ApproveQuoteByTokenCommand>
{
    public ApproveQuoteByTokenCommandValidator() => RuleFor(command => command.Token).NotEmpty()
        .WithError(ServiceOrderErrors.InvalidApprovalToken);
}
