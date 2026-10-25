using MediatR;
using Oficina.Application.Jobs.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;

namespace Oficina.Application.Jobs.Queries.GetServiceHistory;

public sealed class GetServiceHistoryQueryHandler(ICustomerRepository customers, ICustomerInsightQueries insights) : IRequestHandler<GetServiceHistoryQuery, Result<ServiceHistoryDto>>
{
    public async Task<Result<ServiceHistoryDto>> Handle(GetServiceHistoryQuery query, CancellationToken cancellationToken) =>
        await customers.GetByIdAsync(query.CustomerId, cancellationToken) is null
            ? CustomerErrors.NotFound
            : new ServiceHistoryDto(await insights.HistoryAsync(query.CustomerId, cancellationToken));
}
