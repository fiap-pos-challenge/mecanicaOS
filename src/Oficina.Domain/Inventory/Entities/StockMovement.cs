using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Enums;

namespace Oficina.Domain.Inventory.Entities;

public sealed class StockMovement : Entity
{
    private StockMovement(
        Guid id,
        Guid partId,
        StockMovementType type,
        Quantity quantity,
        decimal balanceAfter,
        string reason,
        Guid? serviceOrderId,
        Guid? userId)
        : base(id)
    {
        PartId = partId;
        Type = type;
        Quantity = quantity;
        BalanceAfter = balanceAfter;
        Reason = reason;
        ServiceOrderId = serviceOrderId;
        UserId = userId;
    }

    public Guid PartId { get; private set; }

    public Guid? ServiceOrderId { get; private set; }

    public Guid? UserId { get; private set; }

    public StockMovementType Type { get; private set; }

    public Quantity Quantity { get; private set; }

    public decimal BalanceAfter { get; private set; }

    public string Reason { get; private set; }

    internal static StockMovement Record(
        Guid partId,
        StockMovementType type,
        Quantity quantity,
        decimal balanceAfter,
        string reason,
        Guid? serviceOrderId,
        Guid? userId) =>
        new(Guid.CreateVersion7(), partId, type, quantity, balanceAfter, reason, serviceOrderId, userId);
}
