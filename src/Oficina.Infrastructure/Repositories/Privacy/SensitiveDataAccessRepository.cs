using Oficina.Domain.Privacy.Entities;
using Oficina.Domain.Privacy.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Privacy;

public sealed class SensitiveDataAccessRepository(OficinaDbContext context) : ISensitiveDataAccessRepository
{
    public void Add(SensitiveDataAccess access) => context.SensitiveDataAccesses.Add(access);
}
