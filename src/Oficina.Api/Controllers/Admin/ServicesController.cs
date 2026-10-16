using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Catalog;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Catalog.Commands.CreateService;
using Oficina.Application.Catalog.Commands.DeactivateService;
using Oficina.Application.Catalog.Commands.ReactivateService;
using Oficina.Application.Catalog.Commands.UpdateService;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Queries.GetServiceById;
using Oficina.Application.Catalog.Queries.ListServices;
using Oficina.Application.Common.Pagination;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/services")]
[Tags("Services")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class ServicesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("List services")]
    [EndpointDescription("Returns a page of services ordered by name. Only active services are listed unless includeInactive is true. Roles: Admin, Attendant.")]
    [ProducesResponseType<PagedResult<ServiceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListServicesQuery(page, pageSize, includeInactive), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Create service")]
    [EndpointDescription("Creates an active catalog service with name, optional description, base price greater than zero and estimated time in minutes greater than zero. Role: Admin.")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(CreateServiceRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateServiceCommand(request.Name, request.Description, request.BasePrice, request.EstimatedMinutes);
        return Respond(await sender.Send(command, cancellationToken), service => CreatedAtAction(nameof(GetById), new { id = service.Id }, service));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Get service")]
    [EndpointDescription("Returns one service by id, active or inactive (see isActive). Roles: Admin, Attendant.")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetServiceByIdQuery(id), cancellationToken), Ok);

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Update service")]
    [EndpointDescription("Updates name, description, base price and estimated time of an active service. Inactive services must be reactivated first. Items already added to service orders keep their copied price. Role: Admin.")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateServiceCommand(id, request.Name, request.Description, request.BasePrice, request.EstimatedMinutes);
        return Respond(await sender.Send(command, cancellationToken), Ok);
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Deactivate service")]
    [EndpointDescription("Hides the service from the default listing, blocks changes and blocks new service order items. Items already added to service orders are kept. Role: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeactivateServiceCommand(id), cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Reactivate service")]
    [EndpointDescription("Returns an inactive service to the default listing and allows changes and new service order items again. Role: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ReactivateServiceCommand(id), cancellationToken));
}
