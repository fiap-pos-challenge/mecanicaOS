namespace Oficina.Domain.Reports;

public interface ISavedReportRepository
{
    Task<IReadOnlyList<SavedReport>> ListAsync(CancellationToken cancellationToken);

    Task<SavedReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken);

    void Add(SavedReport report);

    void Remove(SavedReport report);
}
