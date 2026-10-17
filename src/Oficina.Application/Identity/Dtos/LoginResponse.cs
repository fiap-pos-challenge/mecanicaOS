namespace Oficina.Application.Identity.Dtos;

public sealed record LoginResponse(string? AccessToken, DateTimeOffset? ExpiresAt, bool RequiresTwoFactor, Guid? ChallengeId);
