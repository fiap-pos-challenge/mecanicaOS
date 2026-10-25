using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Settings.Entities;
using Oficina.Domain.Settings.Repositories;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Repositories.Quotes;

public sealed class SettingsRepository(OficinaDbContext context) : ISettingsRepository
{
    public Task<SettingsDocument?> GetAsync(string key, CancellationToken cancellationToken) =>
        context.SettingsDocuments.SingleOrDefaultAsync(document => document.Key == key, cancellationToken);

    public async Task<IReadOnlyList<SettingsDocument>> ListAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken) =>
        await context.SettingsDocuments.Where(document => keys.Contains(document.Key)).ToListAsync(cancellationToken);

    public void Add(SettingsDocument document) => context.SettingsDocuments.Add(document);
}
