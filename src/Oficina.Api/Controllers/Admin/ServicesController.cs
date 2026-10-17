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
using Oficina.Application.Catalog.Queries.GetServicePriceHistory;
using Oficina.Application.Catalog.Queries.ListServices;
using Oficina.Application.Common.Pagination;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/services")]
[Tags("Services")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class ServicesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List services")]
    [EndpointDescription("Returns a page of services ordered by name. The term filters by name or description. The status filters active or inactive services; without status, only active services are listed unless includeInactive is true. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<PagedResult<ServiceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? term = null,
        [FromQuery] CatalogStatus? status = null,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListServicesQuery(page, pageSize, includeInactive, term, status), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Create service")]
    [EndpointDescription("Creates an active catalog service with name, optional description, base price greater than zero, estimated time in minutes greater than zero, commission from 0 to 50 percent, warranty from 0 to 3650 days and the parts usually used (existing parts, no repetition). The first price goes to the price history. Roles: Admin, Manager.")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(CreateServiceRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateServiceCommand(request.Name, request.Description, request.BasePrice, request.EstimatedMinutes, request.CommissionPercent, request.WarrantyDays, UsualParts(request.UsualParts));
        return Respond(await sender.Send(command, cancellationToken), service => CreatedAtAction(nameof(GetById), new { id = service.Id }, service));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get service")]
    [EndpointDescription("Returns one service by id, active or inactive (see isActive). Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetServiceByIdQuery(id), cancellationToken), Ok);

    [HttpGet("{id:guid}/price-history")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get service price history")]
    [EndpointDescription("Returns the base price changes of one service, newest first, with the author name. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<PriceHistoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> PriceHistory(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetServicePriceHistoryQuery(id), cancellationToken), Ok);

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Update service")]
    [EndpointDescription("Updates name, description, base price, estimated time, commission, warranty and usual parts of an active service. A new base price goes to the price history. Inactive services must be reactivated first. Items already added to service orders keep their copied price. Roles: Admin, Manager.")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateServiceCommand(id, request.Name, request.Description, request.BasePrice, request.EstimatedMinutes, request.CommissionPercent, request.WarrantyDays, UsualParts(request.UsualParts));
        return Respond(await sender.Send(command, cancellationToken), Ok);
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Deactivate service")]
    [EndpointDescription("Hides the service from the default listing, blocks changes and blocks new service order items. Items already added to service orders are kept. A service used by an active package returns 409 catalog.usedByActivePackage with the package names. Roles: Admin, Manager.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeactivateServiceCommand(id), cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Reactivate service")]
    [EndpointDescription("Returns an inactive service to the default listing and allows changes and new service order items again. Roles: Admin, Manager.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ReactivateServiceCommand(id), cancellationToken));

    private static IReadOnlyList<UsualPartInput> UsualParts(IReadOnlyList<UsualPartRequest>? parts) =>
        (parts ?? []).Select(part => new UsualPartInput(part.PartId, part.Quantity)).ToList();
}
