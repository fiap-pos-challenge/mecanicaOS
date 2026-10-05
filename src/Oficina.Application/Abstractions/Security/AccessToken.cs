namespace Oficina.Application.Abstractions.Security;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
