using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Domain.Common.Results;

namespace Oficina.Api.ExceptionHandling;

public static class ProblemResponses
{
    public const string ContentType = "application/problem+json";

    public static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError,
    };

    public static ProblemDetails Create(HttpContext context, Error error) => Create(context, error, StatusCodeFor(error.Type));

    public static ProblemDetails Create(HttpContext context, Error error, int statusCode)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = ProblemTitles.For(statusCode),
            Detail = error.Message,
            Instance = context.Request.Path,
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        if (error.RetryAfter is { } retryAfter)
        {
            var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
            problem.Extensions["retryAfterSeconds"] = seconds;
            context.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        }

        if (error.Fields.Count > 0)
        {
            problem.Extensions["errors"] = error.Fields;
        }

        return problem;
    }

    public static JsonResult ToResult(ProblemDetails problem) =>
        new(problem) { StatusCode = problem.Status, ContentType = ContentType };

    public static async Task WriteAsync(HttpContext context, Error error, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(Create(context, error, statusCode), options: null, ContentType, context.RequestAborted);
    }
}
