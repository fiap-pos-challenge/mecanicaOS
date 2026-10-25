using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Events;
using Oficina.Domain.ServiceOrders.ValueObjects;
using StatusHistoryEntry = Oficina.Domain.ServiceOrders.Entities.StatusHistory;

namespace Oficina.Domain.ServiceOrders.Entities;

public sealed class ServiceOrder : AggregateRoot
{
    public const int ProblemDescriptionMinLength = 3;
    public const int MaxPhotos = 12;

    private readonly List<ServiceItem> _serviceItems;
    private readonly List<PartItem> _partItems;
    private readonly List<Quote> _quotes;
    private readonly List<StatusHistoryEntry> _statusHistory;
    private readonly List<TimeEntry> _timeEntries;

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
        _timeEntries = new List<TimeEntry>();
        Checklist = ChecklistItem.DefaultLabels.Select((label, index) => new ChecklistItem($"item-{index + 1}", label, null)).ToList();
        Diagnosis = string.Empty;
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

    public int Version { get; private set; }

    public Guid? MechanicId { get; private set; }

    public DateTimeOffset? PromisedAt { get; private set; }

    public int? Mileage { get; private set; }

    public string Diagnosis { get; private set; }

    public bool SentToQuote { get; private set; }

    public bool Reopened { get; private set; }

    public List<ChecklistItem> Checklist { get; private set; }

    public int PhotoCount { get; private set; }

    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();

    public bool TimerRunning => _timeEntries.Exists(entry => entry.IsRunning);

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
        Touch();
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
        Touch();
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

    public Result ApproveQuote(QuoteScope acceptedScope, ApprovalToken approvalToken, Guid? changedByUserId, DateTimeOffset now)
    {
        if (Status is ServiceOrderStatus.Received or ServiceOrderStatus.InDiagnosis)
        {
            var submitted = SubmitQuote(acceptedScope, approvalToken, changedByUserId, now);
            if (submitted.IsFailure)
            {
                return submitted;
            }
        }

        if (Status != ServiceOrderStatus.WaitingApproval)
        {
            return QuoteErrors.NotAwaitingApproval;
        }

        Replace(acceptedScope);
        TotalAmount = acceptedScope.Total;
        return ApproveQuote(changedByUserId, now);
    }

    public Result SubmitQuote(QuoteScope scope, ApprovalToken approvalToken, Guid? changedByUserId, DateTimeOffset now)
    {
        if (Status == ServiceOrderStatus.Received)
        {
            MoveTo(ServiceOrderStatus.InDiagnosis, changedByUserId, null, now);
        }

        if (Status == ServiceOrderStatus.WaitingApproval)
        {
            return Result.Success();
        }

        if (Status != ServiceOrderStatus.InDiagnosis)
        {
            return QuoteErrors.NotAwaitingApproval;
        }

        Replace(scope);
        var sent = SendQuote(approvalToken, changedByUserId, now);
        if (sent.IsFailure)
        {
            return sent;
        }

        TotalAmount = scope.Total;
        return Result.Success();
    }

    public Result RejectQuoteAnswer(string reason, Guid? changedByUserId, DateTimeOffset now) => Status switch
    {
        ServiceOrderStatus.WaitingApproval => RejectQuote(reason, changedByUserId, now),
        ServiceOrderStatus.Received or ServiceOrderStatus.InDiagnosis => Result.Success(),
        _ => QuoteErrors.NotAwaitingApproval,
    };

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
        StopTimer(now);
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

    public void Plan(DateTimeOffset? promisedAt, int? mileage)
    {
        PromisedAt = promisedAt;
        Mileage = mileage;
        Touch();
    }

    public void AssignMechanic(Guid mechanicId)
    {
        if (MechanicId != mechanicId)
        {
            MechanicId = mechanicId;
            Touch();
        }
    }

    public Result StartTimer(Guid mechanicId, DateTimeOffset now)
    {
        if (Status is not (ServiceOrderStatus.InDiagnosis or ServiceOrderStatus.InProgress))
        {
            return ServiceOrderErrors.InvalidTransition;
        }

        MechanicId ??= mechanicId;
        if (!TimerRunning)
        {
            _timeEntries.Add(TimeEntry.Start(Id, mechanicId, now));
            Touch();
        }

        return Result.Success();
    }

    public void StopTimer(DateTimeOffset now)
    {
        foreach (var entry in _timeEntries.Where(entry => entry.IsRunning))
        {
            entry.Stop(now);
            Touch();
        }
    }

    public Result UpdateChecklist(IReadOnlyCollection<(string Id, string? State)> changes)
    {
        if (changes.Any(change => change.State is not null && !ChecklistItem.States.Contains(change.State)))
        {
            return ServiceOrderErrors.InvalidChecklist;
        }

        Checklist = Checklist
            .Select(item => changes.FirstOrDefault(change => change.Id == item.Id) is { Id: not null } change ? item with { State = change.State } : item)
            .ToList();
        Touch();
        return Result.Success();
    }

    public Result RegisterPhoto()
    {
        if (PhotoCount >= MaxPhotos)
        {
            return ServiceOrderErrors.TooManyPhotos;
        }

        PhotoCount++;
        Touch();
        return Result.Success();
    }

    public void WriteDiagnosis(string description)
    {
        Diagnosis = description.Trim();
        Touch();
    }

    public Result MarkSentToQuote(string? description)
    {
        if (Status != ServiceOrderStatus.InDiagnosis)
        {
            return ServiceOrderErrors.ItemsOnlyInDiagnosis;
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            Diagnosis = description.Trim();
        }

        SentToQuote = true;
        Touch();
        return Result.Success();
    }

    public bool HasItem(Guid catalogItemId) =>
        _serviceItems.Exists(item => item.ServiceId == catalogItemId) || _partItems.Exists(item => item.PartId == catalogItemId);

    private void Replace(QuoteScope scope)
    {
        _serviceItems.Clear();
        _partItems.Clear();
        foreach (var (service, quantity) in scope.Services)
        {
            _serviceItems.Add(ServiceItem.Create(Id, service.Id, service.Name, service.BasePrice, quantity));
        }

        foreach (var (part, quantity) in scope.Parts)
        {
            _partItems.Add(PartItem.Create(Id, part.Id, part.Name, part.UnitPrice, quantity));
        }

        Touch();
    }

    private void Touch() => Version++;

    private void MoveTo(ServiceOrderStatus newStatus, Guid? changedByUserId, string? note, DateTimeOffset now)
    {
        _statusHistory.Add(StatusHistoryEntry.Record(Id, Status, newStatus, changedByUserId, note, now));
        Status = newStatus;
        Touch();
        Raise(new ServiceOrderStatusChanged(Id, Number.Value, newStatus.ToString(), now));
    }
}
