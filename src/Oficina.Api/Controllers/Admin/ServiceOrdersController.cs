using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.ServiceOrders;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.ServiceOrders.Commands.AddPhoto;
using Oficina.Application.ServiceOrders.Commands.ApproveQuote;
using Oficina.Application.ServiceOrders.Commands.FinishService;
using Oficina.Application.ServiceOrders.Commands.OpenServiceOrder;
using Oficina.Application.ServiceOrders.Commands.SaveDiagnosis;
using Oficina.Application.ServiceOrders.Commands.SendToQuote;
using Oficina.Application.ServiceOrders.Commands.StartService;
using Oficina.Application.ServiceOrders.Commands.StartTimer;
using Oficina.Application.ServiceOrders.Commands.StopTimer;
using Oficina.Application.ServiceOrders.Commands.TransitionServiceOrder;
using Oficina.Application.ServiceOrders.Commands.UpdateChecklist;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries.GetServiceOrderById;
using Oficina.Application.ServiceOrders.Queries.GetWork;
using Oficina.Application.ServiceOrders.Queries.ListMyWork;
using Oficina.Application.ServiceOrders.Queries.ListServiceOrders;
using Oficina.Application.ServiceOrders.Queries.SuggestItems;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/service-orders")]
[Tags("Service Orders")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class ServiceOrdersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List service orders")]
    [EndpointDescription("Returns a page of service orders from the newest to the oldest, optionally filtered by status and by exact license plate. Roles: Admin, Manager, Attendant, Mechanic.")]
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
    [EndpointDescription("Returns one service order with customer, vehicle, services, parts, current quote, status history and dates. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetServiceOrderByIdQuery(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/quote/approve")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Approve quote")]
    [EndpointDescription("Approves the pending quote in person, deducts the parts from stock and moves the service order to InProgress in the same transaction. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> ApproveQuote([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ApproveQuoteCommand(id), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Open service order")]
    [EndpointDescription("Opens a service order in Received for an existing customer and one of the customer's vehicles, with the problem described by the customer, an optional promised date and the mileage at intake (defaults to the vehicle mileage). A vehicle with an open order gets 409 serviceOrder.vehicleHasOpenOrder. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Open(OpenServiceOrderRequest request, CancellationToken cancellationToken) =>
        Respond(
            await sender.Send(new OpenServiceOrderCommand(request.CustomerId, request.VehicleId, request.ProblemDescription, request.PromisedAt, request.Mileage), cancellationToken),
            order => CreatedAtAction(nameof(GetById), new { id = order.Id }, order));

    [HttpPost("{id:guid}/transitions")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Move service order")]
    [EndpointDescription("Moves the order to the next status with the version the client saw: Received to InDiagnosis, InDiagnosis to WaitingApproval (sends the quote; needs items), WaitingApproval to InProgress (approves and deducts stock) or back to InDiagnosis (rejects; reason with 5 or more characters), InProgress to Completed and Completed to Delivered. Stale version: 409 serviceOrders.versionConflict; other moves: 422 serviceOrder.invalidTransition. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Transition(Guid id, TransitionRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new TransitionServiceOrderCommand(id, request.To, request.Version, request.Reason), cancellationToken), Ok);

    [HttpPost("{id:guid}/time-entries/start")]
    [Authorize(Policy = Policies.Mechanics)]
    [EndpointSummary("Start timer")]
    [EndpointDescription("Starts the work timer of the signed-in mechanic on an order in InDiagnosis or InProgress. Does nothing when a timer is already running. Roles: Admin, Manager, Mechanic.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> StartTimer(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new StartTimerCommand(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/time-entries/stop")]
    [Authorize(Policy = Policies.Mechanics)]
    [EndpointSummary("Stop timer")]
    [EndpointDescription("Stops the running work timer and records the minutes. Roles: Admin, Manager, Mechanic.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> StopTimer(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new StopTimerCommand(id), cancellationToken), Ok);

    [HttpGet("mine")]
    [Authorize(Policy = Policies.Mechanics)]
    [EndpointSummary("My work")]
    [EndpointDescription("Returns the open orders of the signed-in mechanic plus the unassigned ones waiting for a mechanic, by promised date. Roles: Admin, Manager, Mechanic.")]
    [ProducesResponseType<MechanicOrderListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListMyWorkQuery(), cancellationToken), Ok);

    [HttpGet("{id:guid}/work")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get work sheet")]
    [EndpointDescription("Returns the mechanic sheet of an order: complaint, checklist, photos, diagnosis, sent to quote and running timer. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<MechanicOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Work(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetWorkQuery(id), cancellationToken), Ok);

    [HttpPut("{id:guid}/checklist")]
    [Authorize(Policy = Policies.Mechanics)]
    [EndpointSummary("Update checklist")]
    [EndpointDescription("Marks checklist items as Ok, Attention, Replace or empty (null). Unknown item ids are ignored. Roles: Admin, Manager, Mechanic.")]
    [ProducesResponseType<MechanicOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Checklist(Guid id, ChecklistRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new UpdateChecklistCommand(id, request.Items ?? []), cancellationToken), Ok);

    [HttpPost("{id:guid}/photos")]
    [Authorize(Policy = Policies.Mechanics)]
    [EndpointSummary("Add photo")]
    [EndpointDescription("Adds a JPG, PNG or WebP photo as a data URL up to 5 MB. The clientId makes the upload idempotent for the offline queue. Roles: Admin, Manager, Mechanic.")]
    [RequestSizeLimit(8_000_000)]
    [ProducesResponseType<MechanicOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Photo(Guid id, PhotoRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new AddPhotoCommand(id, request.ClientId, request.FileName ?? string.Empty, request.ContentType ?? string.Empty, request.DataUrl ?? string.Empty), cancellationToken), Ok);

    [HttpPut("{id:guid}/diagnosis")]
    [Authorize(Policy = Policies.Mechanics)]
    [EndpointSummary("Save diagnosis")]
    [EndpointDescription("Saves the diagnosis text written by the mechanic. Roles: Admin, Manager, Mechanic.")]
    [ProducesResponseType<MechanicOrderDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Diagnosis(Guid id, DiagnosisRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SaveDiagnosisCommand(id, request.Description ?? string.Empty), cancellationToken), Ok);

    [HttpPost("{id:guid}/diagnosis/suggestions")]
    [Authorize(Policy = Policies.Mechanics)]
    [EndpointSummary("Suggest items")]
    [EndpointDescription("Suggests active catalog services and parts for the described symptoms (suspension, brakes, oil, ignition), with confidence and reason. Roles: Admin, Manager, Mechanic.")]
    [ProducesResponseType<SuggestedItemListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Suggestions(Guid id, DiagnosisRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SuggestItemsQuery(id, request.Description ?? string.Empty), cancellationToken), Ok);

    [HttpPost("{id:guid}/send-to-quote")]
    [Authorize(Policy = Policies.Mechanics)]
    [EndpointSummary("Send to quote")]
    [EndpointDescription("Adds the chosen catalog services and parts (quantity 1, not repeated) to an order in InDiagnosis and marks it ready for the desk to send the quote. Roles: Admin, Manager, Mechanic.")]
    [ProducesResponseType<MechanicOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> SendToQuote(Guid id, SendToQuoteRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SendToQuoteCommand(id, request.Description, request.Items ?? []), cancellationToken), Ok);

    [HttpPost("{id:guid}/start-service")]
    [Authorize(Policy = Policies.Mechanics)]
    [EndpointSummary("Start service")]
    [EndpointDescription("Takes the order for the signed-in mechanic. A Received order moves to InDiagnosis; an InProgress order starts the timer. Roles: Admin, Manager, Mechanic.")]
    [ProducesResponseType<MechanicOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> StartService(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new StartServiceCommand(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/finish-service")]
    [Authorize(Policy = Policies.Mechanics)]
    [EndpointSummary("Finish service")]
    [EndpointDescription("Stops the timer and moves an InProgress order to Completed. Roles: Admin, Manager, Mechanic.")]
    [ProducesResponseType<MechanicOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> FinishService(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new FinishServiceCommand(id), cancellationToken), Ok);
}
