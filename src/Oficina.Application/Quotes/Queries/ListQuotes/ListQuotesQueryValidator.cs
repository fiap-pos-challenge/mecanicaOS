using FluentValidation;
using Oficina.Domain.Quotes.Entities;

namespace Oficina.Application.Quotes.Queries.ListQuotes;

public sealed class ListQuotesQueryValidator : AbstractValidator<ListQuotesQuery>
{
    private static readonly string[] Statuses = [EstimateStatus.Pending, EstimateStatus.Approved, EstimateStatus.Rejected];

    public ListQuotesQueryValidator()
    {
        RuleFor(query => query.Status).Must(status => status is null || Statuses.Contains(status)).WithErrorCode(QuoteValidationCodes.Status).WithMessage(QuoteValidationCodes.StatusMessage);
    }
}
