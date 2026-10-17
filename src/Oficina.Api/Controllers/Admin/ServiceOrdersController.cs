using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.ServiceOrders.Commands.ApproveQuote;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries.GetServiceOrderById;
using Oficina.Application.ServiceOrders.Queries.ListServiceOrders;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/service-orders")]
[Tags("Service Orders")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class ServiceOrdersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List service orders")]
    [EndpointDescription("Returns a page of service orders from the newest to the oldest, optionally filtered by status and by exact license plate. Roles: Admin, Attendant, Mechanic.")]
    [ProducesResponseType<PagedResult<ServiceOrderSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? status = null,
        [FromQuery] string? plate = null,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListServiceOrdersQuery(page, pageSize, status, plate), cancellationToken), Ok);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get service order")]
    [EndpointDescription("Returns one service order with customer, vehicle, services, parts, current quote, status history and dates. Roles: Admin, Attendant, Mechanic.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetServiceOrderByIdQuery(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/quote/approve")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Approve quote")]
    [EndpointDescription("Approves the pending quote in person, deducts the parts from stock and moves the service order to InProgress in the same transaction. Roles: Admin, Attendant.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> ApproveQuote([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ApproveQuoteCommand(id), cancellationToken), Ok);
}
