using Oficina.Application.Common.Pagination;
using Oficina.Application.Settings.Dtos;

namespace Oficina.Application.Settings;

public interface ISettingsQueries
{
    Task<IReadOnlyList<SessionDto>> SessionsAsync(Guid? currentSessionId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<PagedResult<AuditEntryDto>> AuditAsync(AuditFilter filter, CancellationToken cancellationToken);

    Task<int> ActiveUsersAsync(CancellationToken cancellationToken);
}
