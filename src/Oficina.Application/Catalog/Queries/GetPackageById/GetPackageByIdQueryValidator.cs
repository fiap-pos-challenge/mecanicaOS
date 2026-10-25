using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Catalog.Queries.GetPackageById;

public sealed class GetPackageByIdQueryValidator : AbstractValidator<GetPackageByIdQuery>
{
    public GetPackageByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
