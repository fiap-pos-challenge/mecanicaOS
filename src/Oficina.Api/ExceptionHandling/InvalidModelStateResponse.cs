using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Api.ExceptionHandling;

public static class InvalidModelStateResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var fields = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .Select(entry => new FieldError(ToFieldName(entry.Key), CommonErrors.InvalidFormat.Code, CommonErrors.InvalidFormat.Message))
            .DistinctBy(field => field.Field)
            .ToArray();

        return ProblemResponses.ToResult(ProblemResponses.Create(context.HttpContext, Error.Validation(fields), StatusCodes.Status400BadRequest));
    }

    private static string ToFieldName(string key)
    {
        var path = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key.TrimStart('$');
        return string.Join('.', path.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(JsonNamingPolicy.CamelCase.ConvertName));
    }
}
