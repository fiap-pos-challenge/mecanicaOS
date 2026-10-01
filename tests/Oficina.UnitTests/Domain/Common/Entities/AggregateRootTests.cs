using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Events;

namespace Oficina.UnitTests.Domain.Common.Entities;

public sealed class AggregateRootTests
{
    [Fact]
    public void Constructor_AssignsTheIdGivenByTheFactory()
    {
        var id = Guid.CreateVersion7();

        var aggregate = new SampleAggregate(id);

        Assert.Equal(id, aggregate.Id);
    }

    [Fact]
    public void Constructor_StartsWithEmptyDomainEvents()
    {
        var aggregate = new SampleAggregate(Guid.CreateVersion7());

        Assert.NotNull(aggregate.DomainEvents);
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void Raise_AddsTheEvent_AndClearRemovesIt()
    {
        var aggregate = new SampleAggregate(Guid.CreateVersion7());

        aggregate.DoSomething();

        Assert.Single(aggregate.DomainEvents);
        aggregate.ClearDomainEvents();
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void NewAggregate_IsNotDeleted_AndHasNoUpdateDate()
    {
        var aggregate = new SampleAggregate(Guid.CreateVersion7());

        Assert.False(aggregate.IsDeleted);
        Assert.Null(aggregate.UpdatedAt);
        Assert.Null(aggregate.DeletedAt);
    }

    private sealed class SampleAggregate(Guid id) : AggregateRoot(id)
    {
        public void DoSomething() => Raise(new SampleEvent(DateTimeOffset.MinValue));
    }

    private sealed record SampleEvent(DateTimeOffset OccurredAt) : IDomainEvent;
}
