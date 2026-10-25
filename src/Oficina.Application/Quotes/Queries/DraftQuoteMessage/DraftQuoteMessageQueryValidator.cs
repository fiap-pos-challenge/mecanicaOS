using FluentValidation;

namespace Oficina.Application.Quotes.Queries.DraftQuoteMessage;

public sealed class DraftQuoteMessageQueryValidator : AbstractValidator<DraftQuoteMessageQuery>
{
    public DraftQuoteMessageQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
    }
}
