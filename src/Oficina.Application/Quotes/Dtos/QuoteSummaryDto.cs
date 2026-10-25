namespace Oficina.Application.Quotes.Dtos;

public sealed record QuoteSummaryDto(Guid Id, long Number, int Version, string Status, string CustomerName, string LicensePlate, decimal Total, string? DeliveryStatus, bool ApprovalPending, DateTimeOffset UpdatedAt);
