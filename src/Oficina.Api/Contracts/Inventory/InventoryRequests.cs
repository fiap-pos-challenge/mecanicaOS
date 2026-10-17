using Oficina.Application.Inventory.Dtos;

namespace Oficina.Api.Contracts.Inventory;

public sealed record StockEntryRequest(Guid PartId, decimal Quantity, decimal UnitCost, string? Reason, Guid? SupplierId, string? InvoiceNumber);

public sealed record StockExitRequest(Guid PartId, decimal Quantity, string? Reason, long? ServiceOrderNumber);

public sealed record BarcodeRequest(string Barcode);

public sealed record InvoiceFileRequest(string FileName, string ContentType, string Content);

public sealed record InvoiceConfirmRequest(Guid? SupplierId, string InvoiceNumber, IReadOnlyList<InvoiceConfirmLine>? Lines);

public sealed record OpenCountRequest(string Location);

public sealed record CountLinesRequest(IReadOnlyList<CountLineInput>? Lines);

public sealed record CountDecisionRequest(string? Reason);

public sealed record SupplierRequest(string Name, string? Document, string? Email, string? Phone, int LeadTimeDays);

public sealed record PurchaseOrderRequest(Guid SupplierId, IReadOnlyList<PurchaseOrderLineInput>? Lines);
