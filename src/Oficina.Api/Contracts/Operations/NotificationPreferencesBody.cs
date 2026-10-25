using Oficina.Application.Notifications.Dtos;

namespace Oficina.Api.Contracts.Operations;

public sealed record NotificationPreferencesBody(IReadOnlyList<NotificationPreferenceDto>? Items);
