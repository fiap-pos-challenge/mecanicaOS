using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Catalog;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Catalog.Commands.CreatePart;
using Oficina.Application.Catalog.Commands.DeactivatePart;
using Oficina.Application.Catalog.Commands.ReactivatePart;
using Oficina.Application.Catalog.Commands.UpdatePart;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Queries.GetPartById;
using Oficina.Application.Catalog.Queries.GetPartPriceHistory;
using Oficina.Application.Catalog.Queries.ListParts;
using Oficina.Application.Common.Pagination;
using Oficina.Domain.Identity.Enums;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/parts")]
[Tags("Parts")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class PartsController(ISender sender) : ApiControllerBase
{
    private bool CanSeeCosts => User.IsInRole(nameof(Role.Admin)) || User.IsInRole(nameof(Role.Manager));

    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List parts")]
    [EndpointDescription("Returns a page of parts and supplies ordered by name. The term filters by name or exact code. The status filters active or inactive parts; without status, only active parts are listed unless includeInactive is true. The cost is returned only to Admin and Manager. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<PagedResult<PartDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? term = null,
        [FromQuery] CatalogStatus? status = null,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListPartsQuery(page, pageSize, includeInactive, term, status, CanSeeCosts), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Create part")]
    [EndpointDescription("Creates an active part or supply with unique code, name, type, unit of measure, sale price, cost, minimum stock, optional supplier and optional EAN-13 barcode. The stock starts at zero. The first cost and price go to the price history. Roles: Admin, Manager.")]
    [ProducesResponseType<PartDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(PartRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePartCommand(request.Code, request.Name, request.Type, request.UnitOfMeasure, request.UnitPrice, request.Cost, request.MinimumStock, request.Supplier, request.Barcode);
        return Respond(await sender.Send(command, cancellationToken), part => CreatedAtAction(nameof(GetById), new { id = part.Id }, part));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get part")]
    [EndpointDescription("Returns one part, active or inactive, with stock. The cost is returned only to Admin and Manager. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<PartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetPartByIdQuery(id, CanSeeCosts), cancellationToken), Ok);

    [HttpGet("{id:guid}/price-history")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get part price history")]
    [EndpointDescription("Returns the cost and sale price changes of one part, newest first, with the author name. Cost changes are returned only to Admin and Manager. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<PriceHistoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> PriceHistory(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetPartPriceHistoryQuery(id, CanSeeCosts), cancellationToken), Ok);

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Update part")]
    [EndpointDescription("Updates code, name, type, unit of measure, sale price, cost, minimum stock, supplier and barcode. The stock does not change here. New cost or price values go to the price history. Roles: Admin, Manager.")]
    [ProducesResponseType<PartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, PartRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePartCommand(id, request.Code, request.Name, request.Type, request.UnitOfMeasure, request.UnitPrice, request.Cost, request.MinimumStock, request.Supplier, request.Barcode);
        return Respond(await sender.Send(command, cancellationToken), Ok);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Delete part")]
    [EndpointDescription("Logical delete: same rule as deactivate (the part leaves the default listing and new service orders, history is kept). Already inactive: 409. Used by an active package: 409 catalog.usedByActivePackage. Roles: Admin, Manager.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeactivatePartCommand(id), cancellationToken));

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Deactivate part")]
    [EndpointDescription("Hides the part from the default listing and blocks new service order items. A part used by an active package returns 409 catalog.usedByActivePackage with the package names. Roles: Admin, Manager.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeactivatePartCommand(id), cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Reactivate part")]
    [EndpointDescription("Returns an inactive part to the default listing. Roles: Admin, Manager.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ReactivatePartCommand(id), cancellationToken));
}
