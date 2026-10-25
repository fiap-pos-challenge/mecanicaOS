namespace Oficina.Application.Inventory.Dtos;

public sealed record StockCountDto(
    Guid Id,
    string Location,
    string Status,
    string OpenedBy,
    DateTimeOffset OpenedAt,
    DateTimeOffset? SubmittedAt,
    string? DecidedBy,
    DateTimeOffset? DecidedAt,
    string? RejectionReason,
    IReadOnlyList<CountLineDto> Lines);
