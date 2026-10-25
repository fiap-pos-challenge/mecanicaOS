using Oficina.Domain.Common.Events;

namespace Oficina.Domain.Notifications;

public sealed record NotificationCreated(Guid NotificationId, string Type, string Title, string Body, string? Link, bool AiSuggested, DateTimeOffset OccurredAt) : IDomainEvent;
