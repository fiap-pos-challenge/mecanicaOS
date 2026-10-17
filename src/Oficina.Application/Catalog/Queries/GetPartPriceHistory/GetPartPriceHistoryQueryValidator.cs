using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Queries.GetPartPriceHistory;

public sealed class GetPartPriceHistoryQueryValidator : AbstractValidator<GetPartPriceHistoryQuery>
{
    public GetPartPriceHistoryQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
