namespace Oficina.Application.Inventory.Dtos;

public sealed record SupplierDto(Guid Id, string Name, string? Document, string? Email, string? Phone, int LeadTimeDays, bool IsActive);
