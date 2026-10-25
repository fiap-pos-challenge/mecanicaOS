using Oficina.Domain.Privacy.Entities;

namespace Oficina.Domain.Privacy.Repositories;

public interface ISensitiveDataAccessRepository
{
    void Add(SensitiveDataAccess access);
}
