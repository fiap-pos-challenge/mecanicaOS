using Microsoft.Extensions.Logging;

namespace Oficina.Application.Common.Behaviors;

internal static partial class RequestLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Request {RequestName} succeeded in {ElapsedMilliseconds} ms")]
    public static partial void RequestSucceeded(this ILogger logger, string requestName, long elapsedMilliseconds);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Request {RequestName} failed with {ErrorCode} in {ElapsedMilliseconds} ms")]
    public static partial void RequestFailed(this ILogger logger, string requestName, string errorCode, long elapsedMilliseconds);
}
