namespace Oficina.Application.Inventory.Invoices;

public sealed record Nfe(string SupplierName, string? SupplierDocument, string Number, DateTimeOffset IssuedAt, IReadOnlyList<NfeLine> Lines);
