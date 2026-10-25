namespace Oficina.Application.Jobs.Dtos;

public sealed record ServiceHistoryEntryDto(long Number, string Description, decimal Total, string Status, DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt, string? PaymentMethod);
