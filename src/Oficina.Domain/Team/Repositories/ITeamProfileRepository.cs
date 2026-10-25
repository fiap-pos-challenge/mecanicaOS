using Oficina.Domain.Team.Entities;

namespace Oficina.Domain.Team.Repositories;

public interface ITeamProfileRepository
{
    Task<TeamProfile?> GetAsync(Guid userId, CancellationToken cancellationToken);

    void Add(TeamProfile profile);
}
