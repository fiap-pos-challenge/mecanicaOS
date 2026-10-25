using FluentValidation;

namespace Oficina.Application.Quotes.Queries.GetQuote;

public sealed class GetQuoteQueryValidator : AbstractValidator<GetQuoteQuery>
{
    public GetQuoteQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
    }
}
