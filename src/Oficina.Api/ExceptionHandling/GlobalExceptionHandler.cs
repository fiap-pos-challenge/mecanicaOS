using Microsoft.AspNetCore.Diagnostics;
using Oficina.Domain.Common.Errors;

namespace Oficina.Api.ExceptionHandling;

public sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        LogUnhandledException(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        await ProblemResponses.WriteAsync(httpContext, CommonErrors.Unexpected, StatusCodes.Status500InternalServerError);
        return true;
    }

    [LoggerMessage(EventId = 5000, Level = LogLevel.Error, Message = "Unhandled exception on {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string method, string path);
}
