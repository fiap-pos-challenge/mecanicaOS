using Oficina.Domain.Audit.Entities;
using Oficina.Domain.Audit.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Audit;

public sealed class AuditRepository(OficinaDbContext context) : IAuditRepository
{
    public void Add(AuditEntry entry) => context.AuditEntries.Add(entry);
}
