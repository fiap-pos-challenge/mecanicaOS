using System.Text.Json;
using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Settings.Entities;
using Oficina.Domain.Settings.Repositories;

namespace Oficina.Application.Settings;

public sealed class SettingsStore(ISettingsRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public async Task<T> GetAsync<T>(string key, T fallback, CancellationToken cancellationToken)
    {
        var document = await repository.GetAsync(key, cancellationToken);
        if (document is null)
        {
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(document.Json, Options) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    public async Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(value, Options);
        var now = timeProvider.GetUtcNow();
        if (await repository.GetAsync(key, cancellationToken) is { } document)
        {
            document.Replace(json, currentUser.UserId, now);
        }
        else
        {
            repository.Add(SettingsDocument.Create(key, json, currentUser.UserId, now));
        }
    }
}
