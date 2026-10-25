using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;

namespace Oficina.Domain.Inventory.Entities;

public sealed class PurchaseOrder : AggregateRoot
{
    private PurchaseOrder(Guid id, long number, Guid supplierId)
        : base(id)
    {
        Number = number;
        SupplierId = supplierId;
        Status = PurchaseOrderStatus.Draft;
        Lines = [];
    }

    public long Number { get; private set; }

    public Guid SupplierId { get; private set; }

    public PurchaseOrderStatus Status { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public List<PurchaseOrderLine> Lines { get; private set; }

    public decimal Total => Lines.Sum(line => line.Quantity * line.UnitCost);

    public static Result<PurchaseOrder> Create(long number, Guid supplierId, IReadOnlyCollection<PurchaseOrderLine> lines)
    {
        if (supplierId == Guid.Empty || lines.Count == 0 || lines.Any(line => line.Quantity <= 0 || line.UnitCost < 0))
        {
            return InventoryErrors.InvalidOrder;
        }

        return new PurchaseOrder(Guid.CreateVersion7(), number, supplierId) { Lines = lines.ToList() };
    }

    public Result Queue(DateTimeOffset now)
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            return InventoryErrors.OrderAlreadySent;
        }

        Status = PurchaseOrderStatus.Queued;
        SentAt = now;
        return Result.Success();
    }

    public void MarkSent() => Status = PurchaseOrderStatus.Sent;
}
