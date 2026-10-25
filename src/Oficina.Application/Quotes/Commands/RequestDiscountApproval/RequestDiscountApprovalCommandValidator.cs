using FluentValidation;

namespace Oficina.Application.Quotes.Commands.RequestDiscountApproval;

public sealed class RequestDiscountApprovalCommandValidator : AbstractValidator<RequestDiscountApprovalCommand>
{
    public RequestDiscountApprovalCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
    }
}
