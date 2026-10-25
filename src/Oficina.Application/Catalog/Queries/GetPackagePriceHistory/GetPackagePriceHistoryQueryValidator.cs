using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Queries.GetPackagePriceHistory;

public sealed class GetPackagePriceHistoryQueryValidator : AbstractValidator<GetPackagePriceHistoryQuery>
{
    public GetPackagePriceHistoryQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
