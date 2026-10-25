using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Queries.SuggestItems;

public sealed class SuggestItemsQueryValidator : AbstractValidator<SuggestItemsQuery>
{
    public SuggestItemsQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(query => query.Description).Must(description => description?.Trim().Length >= 3).WithError(ServiceOrderErrors.InvalidDescription);
    }
}
