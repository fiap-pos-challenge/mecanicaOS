using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Vehicles;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Vehicles.Commands.CreateVehicle;
using Oficina.Application.Vehicles.Commands.DeleteVehicle;
using Oficina.Application.Vehicles.Commands.TransferVehicle;
using Oficina.Application.Vehicles.Commands.UpdateVehicle;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Application.Vehicles.Queries.GetVehicleById;
using Oficina.Application.Vehicles.Queries.ListCustomerVehicles;
using Oficina.Application.Vehicles.Queries.ListVehicles;
using Oficina.Application.Vehicles.Queries.SearchVehicle;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/vehicles")]
[Tags("Vehicles")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class VehiclesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List vehicles")]
    [EndpointDescription("Returns a page of vehicles ordered by brand. To find a vehicle by license plate, use POST /api/admin/vehicles/search. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<PagedResult<VehicleDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListVehiclesQuery(page, pageSize), cancellationToken), Ok);

    [HttpGet("/api/admin/customers/{customerId:guid}/vehicles")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List customer vehicles")]
    [EndpointDescription("Returns every vehicle of one customer, ordered by brand and model. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<VehicleListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> ListByCustomer([FromRoute] Guid customerId, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListCustomerVehiclesQuery(customerId), cancellationToken), Ok);

    [HttpPost("search")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Find vehicle by license plate")]
    [EndpointDescription("Finds one vehicle by exact license plate. The plate goes in the body, never in the URL. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<VehicleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Search([FromBody] SearchVehicleRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SearchVehicleQuery(request.LicensePlate), cancellationToken), Ok);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get vehicle")]
    [EndpointDescription("Returns one vehicle by id, with its owner and service order history. Roles: Admin, Attendant, Mechanic.")]
    [ProducesResponseType<VehicleDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetVehicleById([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetVehicleByIdQuery(id), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Create vehicle")]
    [EndpointDescription("Creates a vehicle with license plate, brand, model, year and mileage for an existing customer. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<VehicleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateVehicleCommand(request.CustomerId, request.LicensePlate, request.Brand, request.Model, request.Year, request.Mileage);
        return Respond(await sender.Send(command, cancellationToken), vehicle => CreatedAtAction(nameof(GetVehicleById), new { id = vehicle.Id }, vehicle));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Update vehicle")]
    [EndpointDescription("Updates brand, model, year and mileage. The license plate cannot change; the owner changes through the transfer endpoint. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<VehicleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> UpdateVehicle([FromRoute] Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateVehicleCommand(id, request.Model, request.Brand, request.Year, request.Mileage);
        return Respond(await sender.Send(command, cancellationToken), Ok);
    }

    [HttpPost("{id:guid}/transfer")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Transfer vehicle")]
    [EndpointDescription("Moves the vehicle to another existing customer, keeping its service order history. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<VehicleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Transfer([FromRoute] Guid id, [FromBody] TransferVehicleRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new TransferVehicleCommand(id, request.NewCustomerId), cancellationToken), Ok);

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
