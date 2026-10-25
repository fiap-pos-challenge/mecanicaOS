using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Oficina.Infrastructure.Messaging.Outbox;

public sealed partial class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatchAsync(stoppingToken);

            try
            {
                await Task.Delay(interval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<OutboxBatchProcessor>();
            await processor.ProcessPendingAsync(stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogBatchFailure(exception.GetType().Name);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox batch failed: {ErrorType}")]
    private partial void LogBatchFailure(string errorType);
}
