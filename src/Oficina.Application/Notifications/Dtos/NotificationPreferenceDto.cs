namespace Oficina.Application.Notifications.Dtos;

public sealed record NotificationPreferenceDto(string Type, bool InApp, bool Email, bool WhatsApp);
