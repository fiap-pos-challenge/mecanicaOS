namespace Oficina.Application.Inventory.Dtos;

public sealed record InvoiceDraftDto(string SupplierName, string? SupplierDocument, Guid? SupplierId, string InvoiceNumber, DateTimeOffset IssuedAt, IReadOnlyList<InvoiceLineDto> Lines);
