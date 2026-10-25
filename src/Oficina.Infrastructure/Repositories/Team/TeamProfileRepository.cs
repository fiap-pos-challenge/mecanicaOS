using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Team.Entities;
using Oficina.Domain.Team.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Team;

public sealed class TeamProfileRepository(OficinaDbContext context) : ITeamProfileRepository
{
    public Task<TeamProfile?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        context.TeamProfiles.SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

    public void Add(TeamProfile profile) => context.TeamProfiles.Add(profile);
}
