using Oficina.Application.Jobs.Dtos;

namespace Oficina.Application.Jobs;

public interface ICustomerInsightQueries
{
    Task<IReadOnlyList<CustomerSummaryDto>> SummariesAsync(IReadOnlyCollection<Guid> customerIds, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceHistoryEntryDto>> HistoryAsync(Guid customerId, CancellationToken cancellationToken);

    Task<CustomerExport?> ExportAsync(Guid customerId, DateTimeOffset now, CancellationToken cancellationToken);
}
