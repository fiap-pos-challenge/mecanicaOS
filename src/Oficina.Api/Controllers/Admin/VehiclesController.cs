using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Vehicles;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Vehicles.Commands.CreateVehicle;
using Oficina.Application.Vehicles.Dto;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/vehicles")]
[Tags("Vehicles")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class VehiclesController(ISender sender) : ApiControllerBase
{

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
        return Respond(await sender.Send(command, cancellationToken), vehicle => CreatedAtAction($"api/admin/vehicles/{vehicle.Id}",new {id = vehicle.Id}, vehicle));
    }
}
