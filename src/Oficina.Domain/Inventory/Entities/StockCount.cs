using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;

namespace Oficina.Domain.Inventory.Entities;

public sealed record CountLine(Guid PartId, decimal SystemQuantity, decimal? CountedQuantity);

public sealed class StockCount : AggregateRoot
{
    private StockCount(Guid id, string location, Guid? openedBy, DateTimeOffset openedAt)
        : base(id)
    {
        Location = location;
        Status = CountStatus.Open;
        OpenedBy = openedBy;
        OpenedAt = openedAt;
        Lines = [];
    }

    public string Location { get; private set; }

    public CountStatus Status { get; private set; }

    public Guid? OpenedBy { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public DateTimeOffset? SubmittedAt { get; private set; }

    public Guid? DecidedBy { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public string? RejectionReason { get; private set; }

    public List<CountLine> Lines { get; private set; }

    public bool IsOpen => Status is CountStatus.Open or CountStatus.PendingApproval;

    public static StockCount Open(string location, IEnumerable<Part> parts, Guid? openedBy, DateTimeOffset now)
    {
        var count = new StockCount(Guid.CreateVersion7(), location, openedBy, now);
        count.Lines = parts.Select(part => new CountLine(part.Id, part.StockQuantity, null)).ToList();
        return count;
    }

    public Result Record(IReadOnlyCollection<(Guid PartId, decimal? Counted)> counts, Func<Guid, Part?> partOf)
    {
        if (Status != CountStatus.Open)
        {
            return InventoryErrors.CountClosed;
        }

        foreach (var (partId, counted) in counts)
        {
            var part = partOf(partId);
            if (part is null || !Lines.Exists(line => line.PartId == partId))
            {
                continue;
            }

            if (counted is { } value && (value < 0 || (part.Type == Enums.PartType.Part && value != decimal.Truncate(value)) || value * 1000 != decimal.Truncate(value * 1000)))
            {
                return InventoryErrors.FractionalPart with { Fields = [new FieldError("lines", "inventory.fractionalPart", InventoryErrors.FractionalPart.Message)] };
            }
        }

        Lines = Lines.Select(line => counts.FirstOrDefault(count => count.PartId == line.PartId) is { PartId: var id } change && id != Guid.Empty
            ? line with { CountedQuantity = change.Counted }
            : line).ToList();
        return Result.Success();
    }

    public Result Submit(DateTimeOffset now)
    {
        if (Status != CountStatus.Open)
        {
            return InventoryErrors.CountClosed;
        }

        if (Lines.Exists(line => line.CountedQuantity is null))
        {
            return InventoryErrors.CountIncomplete;
        }

        Status = CountStatus.PendingApproval;
        SubmittedAt = now;
        return Result.Success();
    }

    public Result Approve(Func<Guid, Part?> partOf, Guid? userId, DateTimeOffset now)
    {
        if (Status != CountStatus.PendingApproval)
        {
            return InventoryErrors.CountNotPending;
        }

        foreach (var line in Lines)
        {
            partOf(line.PartId)?.Adjust(line.CountedQuantity ?? line.SystemQuantity, $"Inventário {Location}", userId);
        }

        Status = CountStatus.Approved;
        DecidedBy = userId;
        DecidedAt = now;
        return Result.Success();
    }

    public Result Reject(string reason, Guid? userId, DateTimeOffset now)
    {
        if (Status != CountStatus.PendingApproval)
        {
            return InventoryErrors.CountNotPending;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return InventoryErrors.RejectionReasonRequired;
        }

        Status = CountStatus.Rejected;
        RejectionReason = reason.Trim();
        DecidedBy = userId;
        DecidedAt = now;
        return Result.Success();
    }
}

public enum CountStatus
{
    Open,
    PendingApproval,
    Approved,
    Rejected,
}
