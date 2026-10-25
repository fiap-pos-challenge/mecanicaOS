namespace Oficina.Api.Contracts.Auth;

public sealed record VerifyTwoFactorRequest(Guid ChallengeId, string Code);
