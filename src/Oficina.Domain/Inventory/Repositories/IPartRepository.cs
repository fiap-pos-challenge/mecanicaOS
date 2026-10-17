using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.ValueObjects;

namespace Oficina.Domain.Inventory.Repositories;

public interface IPartRepository
{
    Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Part>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(PartCode code, Guid? exceptId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Part>> ListActiveInLocationAsync(string location, CancellationToken cancellationToken);

    Task<Part?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken);

    void Add(Part part);
}
