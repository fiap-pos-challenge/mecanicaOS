using FluentValidation;

namespace Oficina.Application.Quotes.Commands.ApproveQuoteAtDesk;

public sealed class ApproveQuoteAtDeskCommandValidator : AbstractValidator<ApproveQuoteAtDeskCommand>
{
    public ApproveQuoteAtDeskCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
    }
}
