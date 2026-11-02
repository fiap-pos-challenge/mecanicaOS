using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Inventory;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Inventory.Commands.CreatePart;
using Oficina.Application.Inventory.Commands.DeactivatePart;
using Oficina.Application.Inventory.Commands.ReactivatePart;
using Oficina.Application.Inventory.Commands.RegisterStockEntry;
using Oficina.Application.Inventory.Commands.UpdatePart;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Queries.GetPartById;
using Oficina.Application.Inventory.Queries.ListParts;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/parts")]
[Tags("Parts and Supplies")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class PartsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List parts and supplies")]
    [EndpointDescription("Returns a page of parts and supplies ordered by name, with stock balance. Only active items are listed unless includeInactive is true. belowMinimum=true lists only items with stock below the minimum. Roles: Admin, Attendant, Mechanic.")]
    [ProducesResponseType<PagedResult<PartDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] bool includeInactive = false,
        [FromQuery] bool belowMinimum = false,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListPartsQuery(page, pageSize, includeInactive, belowMinimum), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Create part or supply")]
    [EndpointDescription("Creates an active part or supply with unique code, name, type (Part or Supply), unit of measure, unit price and minimum stock. The stock starts at zero; use stock entries to add quantity. Role: Admin.")]
    [ProducesResponseType<PartDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(CreatePartRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePartCommand(request.Code, request.Name, request.Type, request.UnitOfMeasure, request.UnitPrice, request.MinimumStock);
        return Respond(await sender.Send(command, cancellationToken), part => CreatedAtAction(nameof(GetById), new { id = part.Id }, part));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get part or supply")]
    [EndpointDescription("Returns one part or supply by id, active or inactive, with its stock movements (entries and service order deductions) from the oldest to the newest. Roles: Admin, Attendant, Mechanic.")]
    [ProducesResponseType<PartDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetPartByIdQuery(id), cancellationToken), Ok);

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Update part or supply")]
    [EndpointDescription("Updates name, type, unit of measure, unit price and minimum stock. The code and the stock balance are not changed here. Items already added to service orders keep their copied price. Role: Admin.")]
    [ProducesResponseType<PartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, UpdatePartRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePartCommand(id, request.Name, request.Type, request.UnitOfMeasure, request.UnitPrice, request.MinimumStock);
        return Respond(await sender.Send(command, cancellationToken), Ok);
    }

    [HttpPost("{id:guid}/stock-entries")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Register stock entry")]
    [EndpointDescription("Adds quantity to the stock of a part or supply and records the movement with the reason and the user. Role: Admin.")]
    [ProducesResponseType<PartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> RegisterStockEntry(Guid id, RegisterStockEntryRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RegisterStockEntryCommand(id, request.Quantity, request.Reason), cancellationToken), Ok);

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Deactivate part or supply")]
    [EndpointDescription("Hides the item from the default listing and blocks new service order items. Items already added to service orders are kept. Role: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeactivatePartCommand(id), cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Reactivate part or supply")]
    [EndpointDescription("Returns an inactive item to the default listing and allows new service order items again. Role: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ReactivatePartCommand(id), cancellationToken));
}
