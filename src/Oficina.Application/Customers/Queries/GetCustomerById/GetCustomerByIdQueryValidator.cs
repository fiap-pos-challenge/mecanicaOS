using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdQueryValidator : AbstractValidator<GetCustomerByIdQuery>
{
    public GetCustomerByIdQueryValidator() =>
        RuleFor(query => query.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
