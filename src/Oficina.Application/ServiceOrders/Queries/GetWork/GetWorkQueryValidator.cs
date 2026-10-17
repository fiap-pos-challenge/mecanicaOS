using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Queries.GetWork;

public sealed class GetWorkQueryValidator : AbstractValidator<GetWorkQuery>
{
    public GetWorkQueryValidator() => RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
