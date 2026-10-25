using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Notifications;

public static class NotificationErrors
{
    public static readonly Error NotFound = new("notifications.notFound", "Notificação não encontrada.", ErrorType.NotFound);

    public static readonly Error InvalidPreferences = new("notifications.preferences", "Preferências inválidas.", ErrorType.Validation,
        [new FieldError("items", "notifications.preferences", "Preferências inválidas.")]);
}
