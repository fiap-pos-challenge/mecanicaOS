using Oficina.Domain.Audit.Entities;

namespace Oficina.Domain.Audit.Repositories;

public interface IAuditRepository
{
    void Add(AuditEntry entry);
}
