using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Reports;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Reports;

public sealed class SavedReportRepository(OficinaDbContext context) : ISavedReportRepository
{
    public async Task<IReadOnlyList<SavedReport>> ListAsync(CancellationToken cancellationToken) =>
        await context.SavedReports.OrderByDescending(report => report.UpdatedAt).ToListAsync(cancellationToken);

    public Task<SavedReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.SavedReports.SingleOrDefaultAsync(report => report.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken) =>
        context.SavedReports.AnyAsync(report => report.Name == name && report.Id != exceptId, cancellationToken);

    public void Add(SavedReport report) => context.SavedReports.Add(report);

    public void Remove(SavedReport report) => context.SavedReports.Remove(report);
}
