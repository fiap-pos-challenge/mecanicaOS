using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Vehicles;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Vehicles.Commands.CreateVehicle;
using Oficina.Application.Vehicles.Commands.DeleteVehicle;
using Oficina.Application.Vehicles.Commands.UpdateVehicle;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Application.Vehicles.Queries.GetVehicleById;
using Oficina.Application.Vehicles.Queries.ListVehicles;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/vehicles")]
[Tags("Vehicles")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class VehiclesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List vehicles")]
    [EndpointDescription("Returns a page of vehicles ordered by brand, optionally filtered by exact license plate. Roles: Admin, Attendant, Mechanic.")]
    [ProducesResponseType<PagedResult<VehicleDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? plate = null,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListVehiclesQuery(page, pageSize, plate), cancellationToken), Ok);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get vehicle")]
    [EndpointDescription("Returns one vehicle by id, with its owner and service order history. Roles: Admin, Attendant, Mechanic.")]
    [ProducesResponseType<VehicleDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetVehicleById([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetVehicleByIdQuery(id), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Create vehicle")]
    [EndpointDescription("Creates a vehicle with license plate, brand, model and year for an existing customer. Roles: Admin, Attendant.")]
    [ProducesResponseType<VehicleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateVehicleCommand(request.CustomerId, request.Plate, request.Brand, request.Model, request.Year);
        return Respond(await sender.Send(command, cancellationToken), vehicle => CreatedAtAction(nameof(GetVehicleById), new { id = vehicle.Id }, vehicle));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Update vehicle")]
    [EndpointDescription("Updates brand, model and year. The license plate and the owning customer cannot change. Roles: Admin, Attendant.")]
    [ProducesResponseType<VehicleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> UpdateVehicle([FromRoute] Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateVehicleCommand(id, request.Model, request.Brand, request.Year);
        return Respond(await sender.Send(command, cancellationToken), Ok);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Delete vehicle")]
    [EndpointDescription("Soft deletes a vehicle without open service orders. Role: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> DeleteVehicle([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeleteVehicleCommand(id), cancellationToken));
}
