using FluentValidation;

namespace Oficina.Application.Quotes.Queries.GetQuoteOptions;

public sealed class GetQuoteOptionsQueryValidator : AbstractValidator<GetQuoteOptionsQuery>
{
    public GetQuoteOptionsQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
    }
}
