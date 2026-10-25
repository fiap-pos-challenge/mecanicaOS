namespace Oficina.Application.Identity.Dtos;

public sealed record TwoFactorSetupDto(string Secret, string OtpAuthUri);
