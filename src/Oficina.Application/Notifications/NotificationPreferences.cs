using Oficina.Application.Notifications.Dtos;
using Oficina.Application.Settings;

namespace Oficina.Application.Notifications;

public sealed class NotificationPreferences(SettingsStore settings)
{
    public static string Key(Guid userId) => $"notif:{userId:N}";

    public async Task<IReadOnlyList<NotificationPreferenceDto>> OfAsync(Guid userId, CancellationToken cancellationToken) =>
        await settings.GetAsync<List<NotificationPreferenceDto>>(Key(userId), [], cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<NotificationPreferenceDto>>> OfManyAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var byKey = await settings.GetManyAsync<List<NotificationPreferenceDto>>(userIds.Select(Key).ToList(), cancellationToken);
        return userIds.ToDictionary(userId => userId, userId => (IReadOnlyList<NotificationPreferenceDto>)(byKey.GetValueOrDefault(Key(userId)) ?? []));
    }

    public Task SaveAsync(Guid userId, IReadOnlyList<NotificationPreferenceDto> items, CancellationToken cancellationToken) =>
        settings.SaveAsync(Key(userId), items.ToList(), cancellationToken);
}
