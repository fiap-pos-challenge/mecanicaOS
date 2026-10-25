using FluentValidation;

namespace Oficina.Application.Quotes.Queries.GetQuoteDelivery;

public sealed class GetQuoteDeliveryQueryValidator : AbstractValidator<GetQuoteDeliveryQuery>
{
    public GetQuoteDeliveryQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
    }
}
