using Microsoft.Extensions.Logging;
using Oficina.Application.Abstractions.Notifications;

namespace Oficina.Infrastructure.Notifications;

public sealed partial class LogNotifier(ILogger<LogNotifier> logger) : INotifier
{
    public Task NotifyServiceOrderCompletedAsync(long serviceOrderNumber, CancellationToken cancellationToken)
    {
        LogServiceOrderCompleted(serviceOrderNumber);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Customer notified that service order {ServiceOrderNumber} is completed")]
    private partial void LogServiceOrderCompleted(long serviceOrderNumber);
}
