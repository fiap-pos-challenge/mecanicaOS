namespace Oficina.Application.Identity.Dtos;

public sealed record LoginResult(LoginResponse Response, string? RefreshToken, DateTimeOffset? RefreshExpiresAt);
