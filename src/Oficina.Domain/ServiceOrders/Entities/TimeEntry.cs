using Oficina.Domain.Common.Entities;

namespace Oficina.Domain.ServiceOrders.Entities;

public sealed class TimeEntry : Entity
{
    private TimeEntry(Guid id, Guid serviceOrderId, Guid mechanicId, DateTimeOffset startedAt)
        : base(id)
    {
        ServiceOrderId = serviceOrderId;
        MechanicId = mechanicId;
        StartedAt = startedAt;
    }

    public Guid ServiceOrderId { get; private set; }

    public Guid MechanicId { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    public int Minutes { get; private set; }

    public bool IsRunning => EndedAt is null;

    internal static TimeEntry Start(Guid serviceOrderId, Guid mechanicId, DateTimeOffset now) =>
        new(Guid.CreateVersion7(), serviceOrderId, mechanicId, now);

    internal void Stop(DateTimeOffset now)
    {
        EndedAt = now;
        Minutes = (int)((now - StartedAt).TotalMinutes + 0.5);
    }
}
