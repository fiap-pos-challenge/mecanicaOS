using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderDetailDto(
    Guid Id,
    long Number,
    ServiceOrderStatus Status,
    int Version,
    string LicensePlate,
    string CustomerName,
    string Vehicle,
    string Summary,
    string? MechanicName,
    DateTimeOffset? PromisedAt,
    bool HasQuoteItems,
    IReadOnlyList<string> Tags,
    DateTimeOffset UpdatedAt,
    string ProblemDescription,
    int? Mileage,
    decimal TotalAmount,
    ServiceOrderCustomerDto Customer,
    ServiceOrderVehicleDto VehicleDetail,
    IReadOnlyList<ServiceOrderServiceItemDto> Services,
    IReadOnlyList<ServiceOrderBoardPartDto> Parts,
    ServiceOrderBoardQuoteDto? Quote,
    IReadOnlyList<ServiceOrderStatusHistoryDto> StatusHistory,
    IReadOnlyList<ServiceOrderPhotoDto> Photos,
    IReadOnlyList<ServiceOrderTimeEntryDto> TimeEntries,
    IReadOnlyList<ServiceOrderDocumentDto> Documents,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ExecutionStartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? DeliveredAt);

public sealed record ServiceOrderBoardPartDto(Guid Id, Guid PartId, string Name, decimal Quantity, string Unit, decimal UnitPrice, decimal Total, bool Reserved, int? LeadTimeDays);

public sealed record ServiceOrderBoardQuoteItemDto(string Name, decimal Quantity, decimal Total);

public sealed record ServiceOrderBoardQuoteDto(
    Guid Id,
    long Number,
    int Version,
    QuoteStatus Status,
    decimal Total,
    decimal ServicesTotal,
    decimal PartsTotal,
    DateTimeOffset SentAt,
    DateTimeOffset? RespondedAt,
    string? RejectionReason,
    IReadOnlyList<ServiceOrderBoardQuoteItemDto> Items);

public sealed record ServiceOrderPhotoDto(Guid Id, string Url, string Caption, DateTimeOffset TakenAt);

public sealed record ServiceOrderTimeEntryDto(Guid Id, Guid MechanicId, string MechanicName, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, int Minutes);

public sealed record ServiceOrderDocumentDto(string Kind, string Code, DateTimeOffset IssuedAt);

public sealed record ChecklistItemDto(string Id, string Label, string? State);

public sealed record MechanicOrderDto(
    Guid Id,
    long Number,
    ServiceOrderStatus Status,
    string Vehicle,
    string LicensePlate,
    string Complaint,
    DateTimeOffset? PromisedAt,
    IReadOnlyList<ChecklistItemDto> Checklist,
    IReadOnlyList<ServiceOrderPhotoDto> Photos,
    string Diagnosis,
    bool SentToQuote,
    bool TimerRunning,
    int Version);

public sealed record MechanicOrderListDto(IReadOnlyList<MechanicOrderDto> Items);

public sealed record SuggestedItemDto(string Kind, Guid ItemId, string Name, decimal Confidence, string Reason);

public sealed record SuggestedItemListDto(IReadOnlyList<SuggestedItemDto> Items);

public sealed record QuoteRequestItem(string Kind, Guid ItemId);

public sealed record ChecklistChange(string Id, string? State);
