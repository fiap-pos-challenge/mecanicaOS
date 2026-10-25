namespace Oficina.Api.Contracts.Inventory;

public sealed record InvoiceFileRequest(string FileName, string ContentType, string Content);
