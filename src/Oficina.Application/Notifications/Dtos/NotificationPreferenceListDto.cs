namespace Oficina.Application.Notifications.Dtos;

public sealed record NotificationPreferenceListDto(IReadOnlyList<NotificationPreferenceDto> Items);
