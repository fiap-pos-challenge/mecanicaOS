using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions.Messaging;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Messaging.Outbox;

public sealed partial class OutboxBatchProcessor(
    OficinaDbContext context,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxBatchProcessor> logger)
{
    private const string HandleMethodName = nameof(IDomainEventHandler<Domain.Common.Events.IDomainEvent>.HandleAsync);

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var messages = await context.OutboxMessages
            .Where(message => message.ProcessedAt == null && message.Attempts < settings.MaxAttempts)
            .OrderBy(message => message.OccurredAt)
            .Take(settings.BatchSize)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var message in messages)
        {
            if (await TryDispatchAsync(message, cancellationToken))
            {
                message.MarkProcessed(timeProvider.GetUtcNow());
                processed++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return processed;
    }

    private async Task<bool> TryDispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (OutboxEventTypes.Find(message.Type) is not { } eventType
            || JsonSerializer.Deserialize(message.Payload, eventType, OutboxMessage.SerializerOptions) is not { } domainEvent)
        {
            message.RecordFailure(OutboxMessage.UnknownEventTypeError);
            LogUnknownEventType(message.Id, message.Type);
            return false;
        }

        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handle = handlerType.GetMethod(HandleMethodName);

        try
        {
            foreach (var handlerImplementation in HandlerTypes(handlerType))
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetServices(handlerType).OfType<object>().First(candidate => candidate.GetType() == handlerImplementation);
                if (handle?.Invoke(handler, new[] { domainEvent, cancellationToken }) is Task handling)
                {
                    await handling;
                }
            }

            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            message.RecordFailure(exception.GetType().Name);
            LogHandlerFailure(message.Id, message.Type, message.Attempts, exception.GetType().Name);
            return false;
        }
    }

    private IReadOnlyList<Type> HandlerTypes(Type handlerType)
    {
        using var scope = scopeFactory.CreateScope();
        return scope.ServiceProvider.GetServices(handlerType).OfType<object>().Select(handler => handler.GetType()).Distinct().ToList();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {MessageId} has unknown event type {EventType}")]
    private partial void LogUnknownEventType(Guid messageId, string eventType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {MessageId} ({EventType}) failed on attempt {Attempt}: {ErrorType}")]
    private partial void LogHandlerFailure(Guid messageId, string eventType, int attempt, string errorType);
}
