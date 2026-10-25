using Oficina.Domain.Common.Events;

namespace Oficina.Domain.Portal.Events;

public sealed record PaymentConfirmed(Guid ChargeId, Guid EstimateId, Guid ServiceOrderId, string Method, decimal Amount, DateTimeOffset OccurredAt) : IDomainEvent;
