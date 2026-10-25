using Oficina.Domain.Common.Entities;
using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.Domain.ServiceOrders.Entities;

public sealed class StatusHistory : Entity
{
    private StatusHistory(
        Guid id,
        Guid serviceOrderId,
        ServiceOrderStatus? previousStatus,
        ServiceOrderStatus newStatus,
        Guid? changedByUserId,
        string? note,
        DateTimeOffset changedAt)
        : base(id)
    {
        ServiceOrderId = serviceOrderId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId;
        Note = note;
        ChangedAt = changedAt;
    }

    public Guid ServiceOrderId { get; private set; }

    public ServiceOrderStatus? PreviousStatus { get; private set; }

    public ServiceOrderStatus NewStatus { get; private set; }

    public Guid? ChangedByUserId { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }

    internal static StatusHistory Record(
        Guid serviceOrderId,
        ServiceOrderStatus? previousStatus,
        ServiceOrderStatus newStatus,
        Guid? changedByUserId,
        string? note,
        DateTimeOffset changedAt) =>
        new(Guid.CreateVersion7(), serviceOrderId, previousStatus, newStatus, changedByUserId, note, changedAt);
}
