using FluentValidation;

namespace Oficina.Application.Quotes.Commands.RetryQuoteDelivery;

public sealed class RetryQuoteDeliveryCommandValidator : AbstractValidator<RetryQuoteDeliveryCommand>
{
    public RetryQuoteDeliveryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
    }
}
