namespace Oficina.Application.Notifications.Dtos;

public sealed record NotificationDto(Guid Id, string Type, string Title, string Body, DateTimeOffset CreatedAt, string? Link, bool AiSuggested, bool Read);
