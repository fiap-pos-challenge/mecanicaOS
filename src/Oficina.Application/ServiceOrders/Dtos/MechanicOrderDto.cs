using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Application.ServiceOrders.Dtos;

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
