using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.ServiceOrders.EventHandlers;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Events;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;

namespace Oficina.FunctionalTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class OutboxTests(SqlServerFixture database) : IAsyncLifetime
{
    private static readonly DateTimeOffset ProcessedAt = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    private readonly INotifier _notifier = Substitute.For<INotifier>();
    private readonly FakeTimeProvider _clock = new(ProcessedAt);

    public async ValueTask InitializeAsync() => await database.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Commit_WritesTheDomainEventsInTheSameTransaction_WithoutPersonalData()
    {
        var order = await SaveCompletedOrderAsync(6001);

        await using var reader = database.CreateContext(TimeProvider.System);
        var messages = await reader.OutboxMessages.OrderBy(m => m.OccurredAt).ToListAsync(TestContext.Current.CancellationToken);
        var types = messages.Select(m => m.Type).ToArray();
        Assert.Contains(nameof(ServiceOrderOpened), types);
        Assert.Contains(nameof(QuoteSent), types);
        Assert.Contains(nameof(QuoteApproved), types);
        Assert.Contains(nameof(ServiceOrderCompleted), types);
        var completed = messages.Single(m => m.Type == nameof(ServiceOrderCompleted));
        Assert.Contains(order.Id.ToString(), completed.Payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("52998224725", completed.Payload, StringComparison.Ordinal);
        Assert.All(messages, message =>
        {
            Assert.Null(message.ProcessedAt);
            Assert.Equal(0, message.Attempts);
        });
    }

    [Fact]
    public async Task FailedCommit_RollsBackTheAggregateAndLeavesNoMessage()
    {
        await SaveCompletedOrderAsync(6002);
        var messagesBefore = await CountMessagesAsync();

        await using var context = database.CreateContext(TimeProvider.System);
        var duplicate = TestData.Customer();
        context.Customers.Add(duplicate);
        var result = await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CommonErrors.DuplicateRecord, result.Error);
        Assert.Equal(messagesBefore, await CountMessagesAsync());
        Assert.Equal(0, await database.CountRowsAsync("customers", duplicate.Id));
    }

    [Fact]
    public async Task FailedCommit_WithEventRaisingAggregate_WritesNoMessage()
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        await using (var setup = database.CreateContext(TimeProvider.System))
        {
            setup.Customers.Add(customer);
            setup.Vehicles.Add(vehicle);
            await new UnitOfWork(setup, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);
        }

        await using var context = database.CreateContext(TimeProvider.System);
        context.ServiceOrders.Add(TestData.ServiceOrder(customer.Id, vehicle.Id, 6100));
        context.Customers.Add(TestData.Customer());
        var result = await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CommonErrors.DuplicateRecord, result.Error);
        Assert.Equal(0, await CountMessagesAsync());
    }

    [Fact]
    public async Task Processor_DeliversServiceOrderCompletedToTheNotifier_AndMarksProcessedAt()
    {
        var order = await SaveCompletedOrderAsync(6003);

        var processed = await ProcessAsync();

        Assert.Equal(4, processed);
        await _notifier.Received(1).NotifyServiceOrderCompletedAsync(order.Number.Value, Arg.Any<CancellationToken>());
        await using var reader = database.CreateContext(TimeProvider.System);
        var messages = await reader.OutboxMessages.ToListAsync(TestContext.Current.CancellationToken);
        Assert.All(messages, message =>
        {
            Assert.Equal(ProcessedAt, message.ProcessedAt);
            Assert.Null(message.Error);
        });
    }

    [Fact]
    public async Task Processor_WhenHandlerFails_IncrementsAttemptsAndStoresOnlyTheErrorType()
    {
        await SaveCompletedOrderAsync(6004);
        _notifier.NotifyServiceOrderCompletedAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("dado sensível 52998224725"));

        await ProcessAsync();

        await using var reader = database.CreateContext(TimeProvider.System);
        var failed = await reader.OutboxMessages.SingleAsync(m => m.Type == nameof(ServiceOrderCompleted), TestContext.Current.CancellationToken);
        Assert.Null(failed.ProcessedAt);
        Assert.Equal(1, failed.Attempts);
        Assert.Equal(nameof(InvalidOperationException), failed.Error);
    }

    [Fact]
    public async Task Processor_StopsRetryingAfterMaxAttempts()
    {
        await SaveCompletedOrderAsync(6005);
        _notifier.NotifyServiceOrderCompletedAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("falha"));

        await ProcessAsync(maxAttempts: 2);
        await ProcessAsync(maxAttempts: 2);
        await ProcessAsync(maxAttempts: 2);

        await using var reader = database.CreateContext(TimeProvider.System);
        var failed = await reader.OutboxMessages.SingleAsync(m => m.Type == nameof(ServiceOrderCompleted), TestContext.Current.CancellationToken);
        Assert.Equal(2, failed.Attempts);
        await _notifier.Received(2).NotifyServiceOrderCompletedAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Processor_WithUnknownEventType_RecordsFailure()
    {
        await using (var context = database.CreateContext(TimeProvider.System))
        {
            context.OutboxMessages.Add(OutboxMessage.Create(new UnknownEvent(ProcessedAt)));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await ProcessAsync();

        await using var reader = database.CreateContext(TimeProvider.System);
        var message = await reader.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Equal(OutboxMessage.UnknownEventTypeError, message.Error);
    }

    [Fact]
    public async Task Processor_RespectsTheBatchSize()
    {
        await SaveCompletedOrderAsync(6006);

        var processed = await ProcessAsync(batchSize: 2);

        Assert.Equal(2, processed);
        Assert.Equal(2, await CountMessagesAsync(onlyPending: true));
    }

    private async Task<ServiceOrder> SaveCompletedOrderAsync(long number)
    {
        var customer = TestData.Customer();
        var vehicle = TestData.Vehicle(customer.Id);
        var service = TestData.Service();
        var order = TestData.ServiceOrder(customer.Id, vehicle.Id, number);
        order.StartDiagnosis(null, TestData.Today);
        order.AddServiceItem(service, 1);
        order.SendQuote(TestData.Token(), null, TestData.Today.AddMinutes(1));
        order.ApproveQuote(null, TestData.Today.AddMinutes(2));
        order.Complete(null, TestData.Today.AddMinutes(3));

        await using var context = database.CreateContext(TimeProvider.System);
        context.Customers.Add(customer);
        context.Vehicles.Add(vehicle);
        context.Services.Add(service);
        context.ServiceOrders.Add(order);
        Assert.True((await new UnitOfWork(context, new OutboxWriter()).CommitAsync(TestContext.Current.CancellationToken)).IsSuccess);
        Assert.Empty(order.DomainEvents);
        return order;
    }

    private async Task<int> ProcessAsync(int batchSize = 50, int maxAttempts = 5)
    {
        await using var provider = new ServiceCollection()
            .AddSingleton(_notifier)
            .AddScoped<IDomainEventHandler<ServiceOrderCompleted>, NotifyCustomerOnServiceOrderCompleted>()
            .BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await using var context = database.CreateContext(TimeProvider.System);
        var options = Options.Create(new OutboxOptions { BatchSize = batchSize, MaxAttempts = maxAttempts, PollingIntervalSeconds = 1 });
        var processor = new OutboxBatchProcessor(context, scope.ServiceProvider, _clock, options, NullLogger<OutboxBatchProcessor>.Instance);
        return await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);
    }

    private async Task<int> CountMessagesAsync(bool onlyPending = false)
    {
        await using var reader = database.CreateContext(TimeProvider.System);
        return onlyPending
            ? await reader.OutboxMessages.CountAsync(m => m.ProcessedAt == null, TestContext.Current.CancellationToken)
            : await reader.OutboxMessages.CountAsync(TestContext.Current.CancellationToken);
    }

    private sealed record UnknownEvent(DateTimeOffset OccurredAt) : Domain.Common.Events.IDomainEvent;
}
