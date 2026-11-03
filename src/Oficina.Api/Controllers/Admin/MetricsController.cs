using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries.GetAverageExecutionTime;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/metrics")]
[Tags("Métricas")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class MetricsController(ISender sender) : ApiControllerBase
{
    [HttpGet("average-execution-time")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Tempo médio de execução")]
    [EndpointDescription("Retorna o tempo médio, em minutos, entre o início da execução e a finalização das ordens de serviço finalizadas ou entregues. Filtro opcional por data de finalização (from e to, inclusivos). Perfil: Admin.")]
    [ProducesResponseType<AverageExecutionTimeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> AverageExecutionTime(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new GetAverageExecutionTimeQuery(from, to), cancellationToken), Ok);
}
