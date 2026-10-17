using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Queries.GetAverageExecutionTime;

public sealed class GetAverageExecutionTimeQueryValidator : AbstractValidator<GetAverageExecutionTimeQuery>
{
    public GetAverageExecutionTimeQueryValidator() =>
        RuleFor(query => query.From)
            .Must((query, from) => from is null || query.To is null || from <= query.To)
            .WithError(ServiceOrderErrors.InvalidPeriod);
}
