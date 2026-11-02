using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Inventory.Queries.GetPartById;

public sealed class GetPartByIdQueryValidator : AbstractValidator<GetPartByIdQuery>
{
    public GetPartByIdQueryValidator() =>
        RuleFor(query => query.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
