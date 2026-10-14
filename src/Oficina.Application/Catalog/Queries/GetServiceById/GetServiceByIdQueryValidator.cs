using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Queries.GetServiceById;

public sealed class GetServiceByIdQueryValidator : AbstractValidator<GetServiceByIdQuery>
{
    public GetServiceByIdQueryValidator() =>
        RuleFor(query => query.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
