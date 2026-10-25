using Oficina.Domain.Settings.Entities;

namespace Oficina.Domain.Settings.Repositories;

public interface ISettingsRepository
{
    Task<SettingsDocument?> GetAsync(string key, CancellationToken cancellationToken);

    Task<IReadOnlyList<SettingsDocument>> ListAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken);

    void Add(SettingsDocument document);
}
