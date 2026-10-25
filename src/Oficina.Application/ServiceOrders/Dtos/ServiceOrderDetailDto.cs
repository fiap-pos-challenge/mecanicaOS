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
