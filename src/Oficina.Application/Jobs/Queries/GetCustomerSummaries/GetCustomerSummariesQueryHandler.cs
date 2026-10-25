using MediatR;
using Oficina.Application.Jobs.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Jobs.Queries.GetCustomerSummaries;

public sealed class GetCustomerSummariesQueryHandler(ICustomerInsightQueries insights, TimeProvider timeProvider) : IRequestHandler<GetCustomerSummariesQuery, Result<CustomerSummaryListDto>>
{
    public const int MaxSummaries = 500;
    public const string NoReturn = "NoReturn";

    public async Task<Result<CustomerSummaryListDto>> Handle(GetCustomerSummariesQuery query, CancellationToken cancellationToken)
    {
        var ids = (query.CustomerIds ?? []).Distinct().ToList();
        if (ids.Count > MaxSummaries)
        {
            return JobErrors.TooManyCustomers;
        }

        var found = await insights.SummariesAsync(ids, timeProvider.GetUtcNow(), cancellationToken);
        return new CustomerSummaryListDto(ids.Select(id => found.FirstOrDefault(summary => summary.CustomerId == id) ?? new CustomerSummaryDto(id, 0, null, 0, 0, 0, 0, NoReturn, false)).ToList());
    }
}
