using MediatR;
using Microsoft.Extensions.Logging;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger, TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = timeProvider.GetTimestamp();

        var response = await next(cancellationToken);

        var elapsedMilliseconds = (long)timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;
        if (response.Error is { } error)
        {
            logger.RequestFailed(requestName, error.Code, elapsedMilliseconds);
        }
        else
        {
            logger.RequestSucceeded(requestName, elapsedMilliseconds);
        }

        return response;
    }
}
