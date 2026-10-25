namespace Oficina.Application.Jobs.Dtos;

public sealed record ExportCustomer(string Name, string Document, string DocumentType, string? Email, string Phone, object? Address, bool ConsentWhatsApp, bool ConsentEmail, DateTimeOffset? ConsentUpdatedAt, DateTimeOffset CreatedAt);
