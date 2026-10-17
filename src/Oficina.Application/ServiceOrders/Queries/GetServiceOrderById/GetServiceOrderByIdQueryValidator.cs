using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Queries.GetServiceOrderById;

public sealed class GetServiceOrderByIdQueryValidator : AbstractValidator<GetServiceOrderByIdQuery>
{
    public GetServiceOrderByIdQueryValidator() => RuleFor(query => query.Id).NotEmpty()
        .WithError(CommonErrors.RequiredField);
}
