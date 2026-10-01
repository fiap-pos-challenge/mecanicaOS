using Oficina.Domain.Inventory.Entities;

namespace Oficina.Domain.Inventory.Repositories;

public interface IPartRepository
{
    Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Part part);
}
