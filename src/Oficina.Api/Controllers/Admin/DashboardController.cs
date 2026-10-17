using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Dashboard;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Dashboard;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/dashboard")]
[Tags("Dashboard")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class DashboardController(DashboardService dashboard) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Dashboard snapshot")]
    [EndpointDescription("Returns revenue (cents), open orders, approval rate and receivable with the previous period, daily revenue split into services and parts, top services, yard by status and attention items. period: today, week or month (Brazil time). Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<DashboardSnapshotDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Snapshot([FromQuery] string? period, CancellationToken cancellationToken) =>
        Respond(await dashboard.SnapshotAsync(period, cancellationToken), Ok);

    [HttpGet("layout")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Get dashboard layout")]
    [EndpointDescription("Returns the card order and visibility saved by the signed-in user. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<DashboardLayoutDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Layout(CancellationToken cancellationToken) =>
        Respond(await dashboard.LayoutAsync(cancellationToken), Ok);

    [HttpPut("layout")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Save dashboard layout")]
    [EndpointDescription("Saves the card order and visibility of the signed-in user (kpis, dailyRevenue, attention, topServices, yard). Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<DashboardLayoutDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> SaveLayout(DashboardLayoutBody request, CancellationToken cancellationToken) =>
        Respond(await dashboard.SaveLayoutAsync(request.Cards, cancellationToken), Ok);

    [HttpPost("attention/{id}/actions")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Act on attention item")]
    [EndpointDescription("Runs the suggested action of an attention item: CreatePurchaseOrder creates a draft purchase order, SendReminder and SendNewDeadline message the customer by e-mail when consented. The item leaves the list for 24 hours. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Act(string id, DashboardActionBody request, CancellationToken cancellationToken) =>
        Respond(await dashboard.ActAsync(id, request.Action, cancellationToken));
}
