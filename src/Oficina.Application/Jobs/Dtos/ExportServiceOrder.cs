namespace Oficina.Application.Jobs.Dtos;

public sealed record ExportServiceOrder(long Number, string Status, DateTimeOffset OpenedAt, decimal Total);
