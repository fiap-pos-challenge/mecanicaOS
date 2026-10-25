using FluentValidation;

namespace Oficina.Application.Quotes.Queries.ListQuoteVersions;

public sealed class ListQuoteVersionsQueryValidator : AbstractValidator<ListQuoteVersionsQuery>
{
    public ListQuoteVersionsQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
    }
}
