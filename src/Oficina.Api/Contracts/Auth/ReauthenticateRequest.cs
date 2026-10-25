namespace Oficina.Api.Contracts.Auth;

public sealed record ReauthenticateRequest(string? Password, string? Code);
