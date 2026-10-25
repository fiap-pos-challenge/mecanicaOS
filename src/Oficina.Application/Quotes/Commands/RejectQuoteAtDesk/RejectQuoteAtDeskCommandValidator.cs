using FluentValidation;

namespace Oficina.Application.Quotes.Commands.RejectQuoteAtDesk;

public sealed class RejectQuoteAtDeskCommandValidator : AbstractValidator<RejectQuoteAtDeskCommand>
{
    public RejectQuoteAtDeskCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
    }
}
