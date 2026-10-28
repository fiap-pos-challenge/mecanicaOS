using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.ServiceOrders;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.ServiceOrders.Commands.AddPartItem;
using Oficina.Application.ServiceOrders.Commands.AddServiceItem;
using Oficina.Application.ServiceOrders.Commands.ApproveQuote;
using Oficina.Application.ServiceOrders.Commands.CompleteServiceOrder;
using Oficina.Application.ServiceOrders.Commands.DeliverServiceOrder;
using Oficina.Application.ServiceOrders.Commands.OpenServiceOrder;
using Oficina.Application.ServiceOrders.Commands.RejectQuote;
using Oficina.Application.ServiceOrders.Commands.RemoveItem;
using Oficina.Application.ServiceOrders.Commands.SendQuote;
using Oficina.Application.ServiceOrders.Commands.StartDiagnosis;
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

    [HttpPost]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Open service order")]
    [EndpointDescription("Opens a service order in Received status. The customer is identified by CPF or CNPJ and the vehicle by license plate, both sent in the body. The vehicle must belong to the customer and cannot have another open service order. Roles: Admin, Attendant.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Open(OpenServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new OpenServiceOrderCommand(request.CustomerDocument, request.LicensePlate, request.ProblemDescription);
        return Respond(await sender.Send(command, cancellationToken), order => CreatedAtAction(nameof(GetById), new { id = order.Id }, order));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get service order")]
    [EndpointDescription("Returns one service order with customer, vehicle, services, parts, current quote, status history and dates. Roles: Admin, Attendant, Mechanic.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetServiceOrderByIdQuery(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/diagnosis/start")]
    [Authorize(Policy = Policies.AdminOrMechanic)]
    [EndpointSummary("Start diagnosis")]
    [EndpointDescription("Moves a Received service order to InDiagnosis. Services and parts can only be added or removed in this status. Roles: Admin, Mechanic.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> StartDiagnosis([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new StartDiagnosisCommand(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/services")]
    [Authorize(Policy = Policies.AdminOrMechanic)]
    [EndpointSummary("Add service")]
    [EndpointDescription("Adds an active catalog service to a service order in diagnosis, copying its current name and base price. Roles: Admin, Mechanic.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> AddService([FromRoute] Guid id, AddServiceItemRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new AddServiceItemCommand(id, request.ServiceId, request.Quantity), cancellationToken), Ok);

    [HttpPost("{id:guid}/parts")]
    [Authorize(Policy = Policies.AdminOrMechanic)]
    [EndpointSummary("Add part or supply")]
    [EndpointDescription("Adds an active part or supply with enough stock to a service order in diagnosis, copying its current name and unit price. The stock is deducted only when the quote is approved. Roles: Admin, Mechanic.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> AddPart([FromRoute] Guid id, AddPartItemRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new AddPartItemCommand(id, request.PartId, request.Quantity), cancellationToken), Ok);

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [Authorize(Policy = Policies.AdminOrMechanic)]
    [EndpointSummary("Remove item")]
    [EndpointDescription("Removes a service or part item from a service order in diagnosis. Roles: Admin, Mechanic.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> RemoveItem([FromRoute] Guid id, [FromRoute] Guid itemId, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RemoveItemCommand(id, itemId), cancellationToken), Ok);

    [HttpPost("{id:guid}/quote/send")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Send quote")]
    [EndpointDescription("Generates the quote from the services and parts of a service order in diagnosis and moves it to WaitingApproval. Returns the single-use approval token to share with the customer, who approves or rejects it on the public quote routes. Roles: Admin, Attendant.")]
    [ProducesResponseType<SentQuoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> SendQuote([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SendQuoteCommand(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/quote/approve")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Approve quote")]
    [EndpointDescription("Approves the pending quote in person, deducts the parts from stock and moves the service order to InProgress in the same transaction. Roles: Admin, Attendant.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> ApproveQuote([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ApproveQuoteCommand(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/quote/reject")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Reject quote")]
    [EndpointDescription("Registers the customer's rejection of the pending quote with the reason and moves the service order back to InDiagnosis, so the items can be adjusted and a new quote sent. Roles: Admin, Attendant.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> RejectQuote([FromRoute] Guid id, RejectQuoteRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RejectQuoteCommand(id, request.Reason), cancellationToken), Ok);

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Policies.AdminOrMechanic)]
    [EndpointSummary("Complete service order")]
    [EndpointDescription("Moves a service order InProgress to Completed, records the completion time used by the average execution time metric and notifies the customer. Roles: Admin, Mechanic.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Complete([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new CompleteServiceOrderCommand(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/deliver")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Deliver vehicle")]
    [EndpointDescription("Moves a Completed service order to Delivered. After delivery the vehicle can have a new service order. Roles: Admin, Attendant.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Deliver([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeliverServiceOrderCommand(id), cancellationToken), Ok);
}
