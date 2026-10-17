using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Finance;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin")]
[Tags("Finance")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class FinanceController(FinanceService finance) : ApiControllerBase
{
    [HttpGet("finance")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Finance summary")]
    [EndpointDescription("Returns received amounts (delivered orders), receivables (completed and not paid), commissions per mechanic, totals by payment method and the latest receipts. Money in cents. period: today, week, month or quarter (Brazil time). Roles: Admin, Manager.")]
    [ProducesResponseType<FinanceSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Summary([FromQuery] string? period, CancellationToken cancellationToken) =>
        Respond(await finance.SummaryAsync(period, cancellationToken), Ok);

    [HttpGet("documents")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("List documents")]
    [EndpointDescription("Lists the issued documents from the newest. Filter by kind (ServiceOrder, Quote, Receipt) and by part of the code. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<PagedResult<DocumentSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Documents([FromQuery] string? kind = null, [FromQuery] string? code = null, [FromQuery] int page = PageRequest.FirstPage, [FromQuery] int pageSize = PageRequest.DefaultPageSize, CancellationToken cancellationToken = default) =>
        Respond(await finance.DocumentsAsync(kind, code, page, pageSize, cancellationToken), Ok);

    [HttpGet("platform")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Platform overview")]
    [EndpointDescription("Returns the plan of the shop, users in use against the limit, monthly usage and the enabled modules. Roles: Admin.")]
    [ProducesResponseType<PlatformDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Platform(CancellationToken cancellationToken) =>
        Respond(await finance.PlatformAsync(cancellationToken), Ok);
}
