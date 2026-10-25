using Oficina.Application.Team.Dtos;

namespace Oficina.Application.Team.Queries;

public interface ITeamQueries
{
    Task<IReadOnlyList<TeamProfileDto>> ListProfilesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductivityDto>> ProductivityAsync(IReadOnlyCollection<Guid> userIds, int periodDays, DateTimeOffset now, CancellationToken cancellationToken);
}
