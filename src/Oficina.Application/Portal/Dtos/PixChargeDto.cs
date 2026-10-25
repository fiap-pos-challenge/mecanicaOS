namespace Oficina.Application.Portal.Dtos;

public sealed record PixChargeDto(Guid PaymentId, string CopyPaste, string QrCodeDataUrl, DateTimeOffset ExpiresAt);
