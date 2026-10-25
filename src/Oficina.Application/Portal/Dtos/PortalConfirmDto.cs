namespace Oficina.Application.Portal.Dtos;

public sealed record PortalConfirmDto(string Status, string? Session, int AttemptsLeft, DateTimeOffset? ExpiresAt)
{
    public const string Confirmed = "Confirmed";
    public const string WrongDigits = "WrongDigits";
}
