namespace Oficina.Application.Portal.Dtos;

public sealed record PortalQuoteDto(long Number, int Version, string Status, IReadOnlyList<PortalQuoteItemDto> Items, decimal DiscountTotal, decimal Total);
