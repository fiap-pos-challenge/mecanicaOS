using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Inventory.Queries.GetStockOverview;

public sealed class GetStockOverviewQueryValidator : AbstractValidator<GetStockOverviewQuery>
{
    public GetStockOverviewQueryValidator()
    {
        RuleFor(request => request.Page).ValidPage();
        RuleFor(request => request.PageSize).ValidPageSize();
        RuleFor(request => request.Situation).Must(situation => situation is null or "Ok" or "Reorder" or "OutOfStock" or "Idle").WithError(CommonErrors.InvalidFormat);
    }
}
