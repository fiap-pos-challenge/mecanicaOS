using FluentValidation;

namespace Oficina.Application.Quotes.Commands.DecideDiscountApproval;

public sealed class DecideDiscountApprovalCommandValidator : AbstractValidator<DecideDiscountApprovalCommand>
{
    public DecideDiscountApprovalCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
        RuleFor(command => command.ApprovalId).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
    }
}
