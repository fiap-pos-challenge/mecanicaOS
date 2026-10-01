using Oficina.Domain.Common.Events;

namespace Oficina.Domain.ServiceOrders.Events;

public sealed record QuoteApproved(
    Guid ServiceOrderId,
    long Number,
    IReadOnlyCollection<ApprovedPart> Parts,
    DateTimeOffset OccurredAt) : IDomainEvent;
