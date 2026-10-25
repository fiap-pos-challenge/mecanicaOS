using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Inventory.Queries.GetCount;

public sealed class GetCountQueryValidator : AbstractValidator<GetCountQuery>
{
    public GetCountQueryValidator()
    {
        RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
