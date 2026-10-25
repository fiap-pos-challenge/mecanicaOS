using Oficina.Application.Common.Pagination;
using Oficina.Application.Finance.Dtos;

namespace Oficina.Application.Finance;

public interface IFinanceQueries
{
    Task<IReadOnlyList<ReceivableFact>> ReceivablesAsync(CancellationToken cancellationToken);

    Task<PagedResult<DocumentSummaryDto>> DocumentsAsync(string? kind, string? search, int page, int pageSize, CancellationToken cancellationToken);

    Task<PlatformUsageDto> UsageAsync(DateTimeOffset monthStart, CancellationToken cancellationToken);

    Task<int> ActiveUsersAsync(CancellationToken cancellationToken);
}
