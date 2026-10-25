namespace Oficina.Application.Settings.Models;

public sealed record MessageSettings(IReadOnlyList<MessageTemplateSettings> Templates, SendWindowSettings Window);
