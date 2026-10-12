using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Vehicles;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Vehicles.Commands.CreateVehicle;
using Oficina.Application.Vehicles.Commands.DeleteVehicle;
using Oficina.Application.Vehicles.Commands.UpdateVehicle;
using Oficina.Application.Vehicles.Dto;
using Oficina.Application.Vehicles.Queries.GetById;
using Oficina.Application.Vehicles.Queries.ListVehicles;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/vehicles")]
[Tags("Vehicles")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class VehiclesController(ISender sender) : ApiControllerBase
{
    [HttpGet("")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("List paged vehicles")]
    [EndpointDescription("Returns a list of paged vehicles")]
    [ProducesResponseType(typeof(List<VehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPagedVehicles([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var query = new ListVehiclesQuery(page, pageSize);
        return Respond(await sender.Send(query, cancellationToken), Ok);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("vehicle")]
    [EndpointDescription("Returns an existant vehicle")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetVehicleById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new GetVehicleByIdQuery(id);
        return Respond(await sender.Send(query, cancellationToken), Ok);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Allow to update vehicle")]
    [EndpointDescription("Update vehicle year, brand and model.")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> UpdateVehicle([FromRoute] Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateVehicleCommand(id,request.Model, request.Brand, request.Year);
        return Respond(await sender.Send(command, cancellationToken));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Allow to delete vehicle")]
    [EndpointDescription("Delete existant vehicle.")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> DeleteVehicle([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteVehicleCommand(id);
        return Respond(await sender.Send(command, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Allow to create vehicle and associate to a customer")]
    [EndpointDescription("Create vehicle using plate, vehicle year, brand, model and customer associated.")]
    [ProducesResponseType<VehicleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateVehicleCommand(request.CustomerId,request.Plate, request.Brand, request.Model, request.Year);
        return Respond(await sender.Send(command, cancellationToken), vehicle => CreatedAtAction(nameof(GetVehicleById),new {id = vehicle.Id}, vehicle));
    }
}
