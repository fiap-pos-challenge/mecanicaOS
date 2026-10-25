using FluentValidation;

namespace Oficina.Application.Quotes.Commands.CreateQuote;

public sealed class CreateQuoteCommandValidator : AbstractValidator<CreateQuoteCommand>
{
    public CreateQuoteCommandValidator()
    {
        RuleFor(command => command.ServiceOrderId).NotEmpty().WithErrorCode(QuoteValidationCodes.ServiceOrder).WithMessage(QuoteValidationCodes.ServiceOrderMessage);
    }
}
