using Microsoft.AspNetCore.Mvc;
using Oficina.Api.ExceptionHandling;
using Oficina.Domain.Common.Results;

namespace Oficina.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected JsonResult ProblemFor(Error error) => ProblemResponses.ToResult(ProblemResponses.Create(HttpContext, error));

    protected IActionResult Respond<T>(Result<T> result, Func<T, IActionResult> onSuccess) =>
        result.Error is { } error ? ProblemFor(error) : onSuccess(result.Value);

    protected IActionResult Respond(Result result) =>
        result.Error is { } error ? ProblemFor(error) : NoContent();
}
