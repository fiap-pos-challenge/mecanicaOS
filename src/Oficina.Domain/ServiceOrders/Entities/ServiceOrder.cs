using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Events;
using Oficina.Domain.ServiceOrders.ValueObjects;
using StatusHistoryEntry = Oficina.Domain.ServiceOrders.Entities.StatusHistory;

namespace Oficina.Domain.ServiceOrders.Entities;

public sealed class ServiceOrder : AggregateRoot
{
    public const int ProblemDescriptionMinLength = 3;

    private readonly List<ServiceItem> _serviceItems;
    private readonly List<PartItem> _partItems;
    private readonly List<Quote> _quotes;
    private readonly List<StatusHistoryEntry> _statusHistory;

    private ServiceOrder(
        Guid id,
        ServiceOrderNumber number,
        Guid customerId,
        Guid vehicleId,
        ServiceOrderStatus status,
        string problemDescription,
        Money totalAmount)
        : base(id)
    {
        Number = number;
        CustomerId = customerId;
        VehicleId = vehicleId;
        Status = status;
        ProblemDescription = problemDescription;
        TotalAmount = totalAmount;
        _serviceItems = new List<ServiceItem>();
        _partItems = new List<PartItem>();
        _quotes = new List<Quote>();
        _statusHistory = new List<StatusHistoryEntry>();
    }

    public ServiceOrderNumber Number { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid VehicleId { get; private set; }

    public ServiceOrderStatus Status { get; private set; }

    public string ProblemDescription { get; private set; }

    public Money TotalAmount { get; private set; }

    public DateTimeOffset? ExecutionStartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public bool IsOpen => Status != ServiceOrderStatus.Delivered;

    public TimeSpan? ExecutionTime => CompletedAt - ExecutionStartedAt;

    public Quote? CurrentQuote => _quotes.MaxBy(quote => quote.Version);

    public IReadOnlyCollection<ServiceItem> ServiceItems => _serviceItems.AsReadOnly();

    public IReadOnlyCollection<PartItem> PartItems => _partItems.AsReadOnly();

    public IReadOnlyCollection<Quote> Quotes => _quotes.AsReadOnly();

    public IReadOnlyCollection<StatusHistoryEntry> StatusHistory => _statusHistory.AsReadOnly();

    public static Result<ServiceOrder> Open(
        ServiceOrderNumber number,
        Guid customerId,
        Guid vehicleId,
        string problemDescription,
        Guid? openedByUserId,
        DateTimeOffset now)
    {
        if (customerId == Guid.Empty)
        {
            return ServiceOrderErrors.CustomerRequired;
        }

        if (vehicleId == Guid.Empty)
        {
            return ServiceOrderErrors.VehicleRequired;
        }

        if (string.IsNullOrWhiteSpace(problemDescription) || problemDescription.Trim().Length < ProblemDescriptionMinLength)
        {
            return ServiceOrderErrors.InvalidProblemDescription;
        }

        var order = new ServiceOrder(
            Guid.CreateVersion7(),
            number,
            customerId,
            vehicleId,
            ServiceOrderStatus.Received,
            problemDescription.Trim(),
            Money.Zero);
        order._statusHistory.Add(StatusHistoryEntry.Record(order.Id, null, ServiceOrderStatus.Received, openedByUserId, null, now));
        order.Raise(new ServiceOrderOpened(order.Id, number.Value, now));
        return order;
    }

    public Result StartDiagnosis(Guid? changedByUserId, DateTimeOffset now)
    {
        if (Status != ServiceOrderStatus.Received)
        {
            return ServiceOrderErrors.InvalidTransition;
        }

        MoveTo(ServiceOrderStatus.InDiagnosis, changedByUserId, null, now);
        return Result.Success();
    }

    public Result AddServiceItem(Service service, int quantity)
    {
        if (Status != ServiceOrderStatus.InDiagnosis)
        {
            return ServiceOrderErrors.ItemsOnlyInDiagnosis;
        }

        if (!service.IsActive)
        {
            return CatalogErrors.ServiceInactive;
        }

        if (quantity <= 0)
        {
            return ServiceOrderErrors.InvalidServiceQuantity;
        }

        _serviceItems.Add(ServiceItem.Create(Id, service.Id, service.Name, service.BasePrice, quantity));
        return Result.Success();
    }

    public Result AddPartItem(Part part, Quantity quantity)
    {
        if (Status != ServiceOrderStatus.InDiagnosis)
        {
            return ServiceOrderErrors.ItemsOnlyInDiagnosis;
        }

        if (!part.IsActive)
        {
            return InventoryErrors.PartInactive;
        }

        if (!part.HasAvailable(quantity))
        {
            return InventoryErrors.PartUnavailable;
        }

        _partItems.Add(PartItem.Create(Id, part.Id, part.Name, part.UnitPrice, quantity));
        return Result.Success();
    }

    public Result RemoveItem(Guid itemId)
    {
        if (Status != ServiceOrderStatus.InDiagnosis)
        {
            return ServiceOrderErrors.ItemsOnlyInDiagnosis;
        }

        var removed = _serviceItems.RemoveAll(item => item.Id == itemId) + _partItems.RemoveAll(item => item.Id == itemId);
        return removed > 0 ? Result.Success() : ServiceOrderErrors.ItemNotFound;
    }

    public Result SendQuote(ApprovalToken approvalToken, Guid? changedByUserId, DateTimeOffset now)
    {
        if (Status != ServiceOrderStatus.InDiagnosis)
        {
            return ServiceOrderErrors.InvalidTransition;
        }

        if (_serviceItems.Count == 0 && _partItems.Count == 0)
        {
            return ServiceOrderErrors.QuoteWithoutItems;
        }

        var servicesTotal = _serviceItems.Aggregate(Money.Zero, (sum, item) => sum.Add(item.Total));
        var partsTotal = _partItems.Aggregate(Money.Zero, (sum, item) => sum.Add(item.Total));
        var quote = Quote.Send(Id, _quotes.Count + 1, servicesTotal, partsTotal, approvalToken, now);
        _quotes.Add(quote);
        TotalAmount = quote.Total;
        MoveTo(ServiceOrderStatus.WaitingApproval, changedByUserId, null, now);
        Raise(new QuoteSent(Id, Number.Value, quote.Version, quote.Total.Amount, now));
        return Result.Success();
    }

    public Result RegenerateApprovalToken(ApprovalToken approvalToken)
    {
        if (Status != ServiceOrderStatus.WaitingApproval || CurrentQuote is not { } quote)
        {
            return ServiceOrderErrors.InvalidTransition;
        }

        quote.ReplaceApprovalToken(approvalToken);
        return Result.Success();
    }

    public bool MatchesApprovalToken(ApprovalToken approvalToken) =>
        Status == ServiceOrderStatus.WaitingApproval && CurrentQuote?.ApprovalToken == approvalToken;

    public Result ApproveQuote(Guid? changedByUserId, DateTimeOffset now)
    {
        if (Status != ServiceOrderStatus.WaitingApproval || CurrentQuote is not { } quote)
        {
            return ServiceOrderErrors.InvalidTransition;
        }

        quote.Approve(now);
        ExecutionStartedAt = now;
        MoveTo(ServiceOrderStatus.InProgress, changedByUserId, null, now);
        var approvedParts = _partItems.Select(item => new ApprovedPart(item.PartId, item.Quantity.Value)).ToList();
        Raise(new QuoteApproved(Id, Number.Value, approvedParts, now));
        return Result.Success();
    }

    public Result RejectQuote(string reason, Guid? changedByUserId, DateTimeOffset now)
    {
        if (Status != ServiceOrderStatus.WaitingApproval || CurrentQuote is not { } quote)
        {
            return ServiceOrderErrors.InvalidTransition;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return ServiceOrderErrors.RejectionReasonRequired;
        }

        var trimmedReason = reason.Trim();
        quote.Reject(trimmedReason, now);
        MoveTo(ServiceOrderStatus.InDiagnosis, changedByUserId, trimmedReason, now);
        Raise(new QuoteRejected(Id, Number.Value, now));
        return Result.Success();
    }

    public Result Complete(Guid? changedByUserId, DateTimeOffset now)
    {
        if (Status != ServiceOrderStatus.InProgress)
        {
            return ServiceOrderErrors.InvalidTransition;
        }

        CompletedAt = now;
        MoveTo(ServiceOrderStatus.Completed, changedByUserId, null, now);
        Raise(new ServiceOrderCompleted(Id, Number.Value, CustomerId, now));
        return Result.Success();
    }

    public Result Deliver(Guid? changedByUserId, DateTimeOffset now)
    {
        if (Status != ServiceOrderStatus.Completed)
        {
            return ServiceOrderErrors.InvalidTransition;
        }

        DeliveredAt = now;
        MoveTo(ServiceOrderStatus.Delivered, changedByUserId, null, now);
        return Result.Success();
    }

    private void MoveTo(ServiceOrderStatus newStatus, Guid? changedByUserId, string? note, DateTimeOffset now)
    {
        _statusHistory.Add(StatusHistoryEntry.Record(Id, Status, newStatus, changedByUserId, note, now));
        Status = newStatus;
    }
}
