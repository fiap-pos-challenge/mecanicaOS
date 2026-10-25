namespace Oficina.Application.Portal.Dtos;

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
