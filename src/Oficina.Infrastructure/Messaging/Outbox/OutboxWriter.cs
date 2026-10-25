using Oficina.Domain.Common.Events;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Messaging.Outbox;

public sealed class OutboxWriter
{
    public void AppendPendingEvents(OficinaDbContext context)
    {
        var aggregates = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            context.OutboxMessages.AddRange(aggregate.DomainEvents.Select(OutboxMessage.Create));
            aggregate.ClearDomainEvents();
        }
    }
}
