namespace Oficina.Api.Contracts.Inventory;

public sealed record SupplierRequest(string Name, string? Document, string? Email, string? Phone, int LeadTimeDays);
