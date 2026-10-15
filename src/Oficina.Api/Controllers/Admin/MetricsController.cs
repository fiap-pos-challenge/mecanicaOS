using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries.GetAverageExecutionTime;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/metrics")]
[Tags("Metrics")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class MetricsController(ISender sender) : ApiControllerBase
{
    [HttpGet("average-execution-time")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Average execution time")]
    [EndpointDescription("Returns the average time in minutes from execution start to completion of completed or delivered service orders, optionally filtered by completion date (from and to, inclusive). Role: Admin.")]
    [ProducesResponseType<AverageExecutionTimeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> AverageExecutionTime(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new GetAverageExecutionTimeQuery(from, to), cancellationToken), Ok);
}
