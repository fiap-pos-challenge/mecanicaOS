using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Settings.Errors;

public static class SettingsErrors
{
    public static readonly Error SessionNotFound = new("settings.sessionNotFound", "Sessão não encontrada.", ErrorType.NotFound);

    public static Error Invalid(string field, string message) =>
        new($"settings.{field}", message, ErrorType.Validation, [new FieldError(field, $"settings.{field}", message)]);
}
