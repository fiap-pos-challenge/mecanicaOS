namespace Oficina.Api.Contracts.Auth;

public sealed record VerifyTwoFactorRequest(Guid ChallengeId, string Code);

public sealed record ReauthenticateRequest(string? Password, string? Code);

public sealed record EnableTwoFactorRequest(string Code);
