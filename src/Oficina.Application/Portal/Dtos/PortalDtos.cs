namespace Oficina.Application.Portal.Dtos;

public sealed record PortalAccessDto(int AttemptsLeft);

public sealed record PortalConfirmDto(string Status, string? Session, int AttemptsLeft, DateTimeOffset? ExpiresAt);

public sealed record PortalHistoryDto(string Status, DateTimeOffset ChangedAt);

public sealed record PortalPhotoDto(Guid Id, string Url, string Caption);

public sealed record PortalQuoteItemDto(Guid Id, string Name, string Kind, decimal Total);

public sealed record PortalQuoteDto(long Number, int Version, string Status, IReadOnlyList<PortalQuoteItemDto> Items, decimal DiscountTotal, decimal Total);

public sealed record PortalPaymentDto(string Status, decimal AmountDue, IReadOnlyList<string> Methods);

public sealed record PortalDocumentDto(string Kind, string Code, DateTimeOffset IssuedAt);

public sealed record PortalOrderDto(
    long Number,
    string ShopName,
    string CustomerFirstName,
    string Vehicle,
    string LicensePlate,
    string Status,
    IReadOnlyList<PortalHistoryDto> StatusHistory,
    IReadOnlyList<PortalPhotoDto> Photos,
    PortalQuoteDto? Quote,
    PortalPaymentDto? Payment,
    IReadOnlyList<PortalDocumentDto> Documents);

public sealed record PixChargeDto(Guid PaymentId, string CopyPaste, string QrCodeDataUrl, DateTimeOffset ExpiresAt);

public sealed record ChargeStatusDto(string Status);

public sealed record CardRedirectDto(string RedirectUrl);

public sealed record DocumentLineDto(string Name, decimal Quantity, decimal UnitPrice, decimal Total);

public sealed record DocumentDto(
    string Kind,
    string Code,
    long Number,
    int? Version,
    DateTimeOffset IssuedAt,
    string ShopName,
    string CustomerName,
    string Vehicle,
    IReadOnlyList<DocumentLineDto> Lines,
    decimal ServicesTotal,
    decimal PartsTotal,
    decimal DiscountTotal,
    decimal Total,
    DateTimeOffset? PaidAt,
    IReadOnlyList<string> PaymentMethods);

public sealed record DocumentDownloadDto(string Url, string FileName);

public sealed record DocumentVerificationDto(bool Valid, string? Kind, long? Number, DateTimeOffset? IssuedAt, decimal? Total, string? ShopName);

public sealed record DocumentSendDto(string Status);
