using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Domain.ServiceOrders.Events;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;

namespace Oficina.FunctionalTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class OutboxProcessorHostingTests(SqlServerFixture database) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await database.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task RunningApi_ProcessesPendingMessagesInTheBackground()
    {
        await using var factory = new FunctionalApiFactory(database);
        using var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<OficinaDbContext>();
            context.OutboxMessages.Add(OutboxMessage.Create(new ServiceOrderCompleted(Guid.CreateVersion7(), 4321, Guid.CreateVersion7(), TestData.Today)));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var processedAt = await WaitForProcessingAsync();

        Assert.NotNull(processedAt);
    }

    private async Task<DateTimeOffset?> WaitForProcessingAsync()
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            await using var reader = database.CreateContext(TimeProvider.System);
            var message = await reader.OutboxMessages.SingleAsync(TestContext.Current.CancellationToken);
            if (message.ProcessedAt is not null)
            {
                return message.ProcessedAt;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);
        }

        return null;
    }
}
