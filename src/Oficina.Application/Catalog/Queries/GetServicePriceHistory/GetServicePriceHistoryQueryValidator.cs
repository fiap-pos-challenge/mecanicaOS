using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Queries.GetServicePriceHistory;

public sealed class GetServicePriceHistoryQueryValidator : AbstractValidator<GetServicePriceHistoryQuery>
{
    public GetServicePriceHistoryQueryValidator() => RuleFor(query => query.ServiceId).NotEmpty()
        .WithError(CommonErrors.RequiredField);
}
