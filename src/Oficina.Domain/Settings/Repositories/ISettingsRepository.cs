using Oficina.Domain.Settings.Entities;

namespace Oficina.Domain.Settings.Repositories;

public interface ISettingsRepository
{
    Task<SettingsDocument?> GetAsync(string key, CancellationToken cancellationToken);

    void Add(SettingsDocument document);
}
