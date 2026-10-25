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
        return document is null ? fallback : Read(document.Json, fallback);
    }

    public async Task<IReadOnlyDictionary<string, T>> GetManyAsync<T>(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
        where T : class
    {
        var documents = await repository.ListAsync(keys, cancellationToken);
        return documents
            .Select(document => (document.Key, Value: Read<T?>(document.Json, null)))
            .Where(item => item.Value is not null)
            .ToDictionary(item => item.Key, item => item.Value!, StringComparer.Ordinal);
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

    private static T Read<T>(string json, T fallback)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Options) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }
}
