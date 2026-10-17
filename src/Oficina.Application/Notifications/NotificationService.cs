using System.Text.Json;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Realtime;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Settings;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Notifications;

namespace Oficina.Application.Notifications;

public sealed record NotificationDto(Guid Id, string Type, string Title, string Body, DateTimeOffset CreatedAt, string? Link, bool AiSuggested, bool Read);

public sealed record NotificationListDto(IReadOnlyList<NotificationDto> Items);

public sealed record NotificationPreferenceDto(string Type, bool InApp, bool Email, bool WhatsApp);

public sealed record NotificationPreferenceListDto(IReadOnlyList<NotificationPreferenceDto> Items);

public interface INotificationPublisher
{
    Task PublishAsync(string type, string title, string body, string? link, bool aiSuggested, string? dedupeKey, CancellationToken cancellationToken);
}

public sealed class NotificationService(
    INotificationRepository notifications,
    IUserDirectory directory,
    SettingsStore settings,
    IRealtimePublisher realtime,
    IEmailSender emailSender,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : INotificationPublisher
{
    public const int ListSize = 50;
    public static readonly string[] Types = ["ServiceOrderChanged", "QuoteAnswered", "PaymentConfirmed", "StockLow", "ExportReady", "AutomationFailed", "DiscountApproval"];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Result<NotificationListDto>> ListAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId.GetValueOrDefault();
        var hidden = (await PreferencesOfAsync(userId, cancellationToken)).Where(item => !item.InApp).Select(item => item.Type).ToHashSet(StringComparer.Ordinal);
        var recent = (await notifications.RecentAsync(ListSize, cancellationToken)).Where(item => !hidden.Contains(item.Type)).ToList();
        var read = await notifications.ReadIdsAsync(userId, recent.Select(item => item.Id).ToList(), cancellationToken);
        return new NotificationListDto(recent.Select(item => new NotificationDto(item.Id, item.Type, item.Title, item.Body, item.CreatedAt, item.Link, item.AiSuggested, read.Contains(item.Id))).ToList());
    }

    public async Task<Result> ReadAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await notifications.GetByIdAsync(id, cancellationToken) is null)
        {
            return NotificationErrors.NotFound;
        }

        var userId = currentUser.UserId.GetValueOrDefault();
        if ((await notifications.ReadIdsAsync(userId, [id], cancellationToken)).Count == 0)
        {
            notifications.Add(NotificationRead.Mark(id, userId, timeProvider.GetUtcNow()));
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task<Result> ReadAllAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId.GetValueOrDefault();
        var recent = await notifications.RecentAsync(ListSize, cancellationToken);
        var read = await notifications.ReadIdsAsync(userId, recent.Select(item => item.Id).ToList(), cancellationToken);
        foreach (var item in recent.Where(item => !read.Contains(item.Id)))
        {
            notifications.Add(NotificationRead.Mark(item.Id, userId, timeProvider.GetUtcNow()));
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task<Result<NotificationPreferenceListDto>> PreferencesAsync(CancellationToken cancellationToken) =>
        new NotificationPreferenceListDto(await PreferencesOfAsync(currentUser.UserId.GetValueOrDefault(), cancellationToken));

    public async Task<Result<NotificationPreferenceListDto>> SavePreferencesAsync(IReadOnlyList<NotificationPreferenceDto>? items, CancellationToken cancellationToken)
    {
        if (items is null || items.Any(item => !Types.Contains(item.Type)) || items.Select(item => item.Type).Distinct(StringComparer.Ordinal).Count() != items.Count)
        {
            return NotificationErrors.InvalidPreferences;
        }

        var saved = items.ToList();
        await settings.SaveAsync(Key(currentUser.UserId.GetValueOrDefault()), saved, cancellationToken);
        return (await unitOfWork.CommitAsync(cancellationToken)).Error is { } error ? error : new NotificationPreferenceListDto(saved);
    }

    public async Task PublishAsync(string type, string title, string body, string? link, bool aiSuggested, string? dedupeKey, CancellationToken cancellationToken)
    {
        if (dedupeKey is not null && await notifications.DedupeExistsAsync(dedupeKey, cancellationToken))
        {
            return;
        }

        var notification = Notification.Create(type, title, body, link, aiSuggested, dedupeKey, timeProvider.GetUtcNow());
        notifications.Add(notification);
        await realtime.PublishAsync(new
        {
            type = "notification",
            notification = new NotificationDto(notification.Id, notification.Type, notification.Title, notification.Body, notification.CreatedAt, notification.Link, notification.AiSuggested, false),
            occurredAt = notification.CreatedAt,
        }, cancellationToken);
        foreach (var (userId, email) in await directory.ActiveEmailsAsync(cancellationToken))
        {
            if ((await PreferencesOfAsync(userId, cancellationToken)).Any(item => item.Type == type && item.Email))
            {
                await emailSender.SendAsync(new EmailMessage(email, notification.Title, $"<p>{System.Net.WebUtility.HtmlEncode(notification.Body)}</p>"), cancellationToken);
            }
        }
    }

    private static string Key(Guid userId) => $"notif:{userId:N}";

    private async Task<List<NotificationPreferenceDto>> PreferencesOfAsync(Guid userId, CancellationToken cancellationToken) =>
        await settings.GetAsync<List<NotificationPreferenceDto>>(Key(userId), [], cancellationToken);
}

public interface IUserDirectory
{
    Task<IReadOnlyList<(Guid UserId, string Email)>> ActiveEmailsAsync(CancellationToken cancellationToken);
}
