using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Quotes;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Quotes.Commands.ApproveQuoteAtDesk;
using Oficina.Application.Quotes.Commands.CreateQuote;
using Oficina.Application.Quotes.Commands.CreateQuoteVersion;
using Oficina.Application.Quotes.Commands.DecideDiscountApproval;
using Oficina.Application.Quotes.Commands.RejectQuoteAtDesk;
using Oficina.Application.Quotes.Commands.RequestDiscountApproval;
using Oficina.Application.Quotes.Commands.RetryQuoteDelivery;
using Oficina.Application.Quotes.Commands.SaveQuoteDraft;
using Oficina.Application.Quotes.Commands.SendQuote;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Queries.DraftQuoteMessage;
using Oficina.Application.Quotes.Queries.GetQuote;
using Oficina.Application.Quotes.Queries.GetQuoteDelivery;
using Oficina.Application.Quotes.Queries.GetQuoteOptions;
using Oficina.Application.Quotes.Queries.ListQuotes;
using Oficina.Application.Quotes.Queries.ListQuoteServiceOrders;
using Oficina.Application.Quotes.Queries.ListQuoteVersions;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/quotes")]
[Tags("Quotes")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class QuotesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("List quotes")]
    [EndpointDescription("Returns the latest version of each quote from the newest, with total, delivery and pending approval. Filter by status (Pending, Approved, Rejected). Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<PagedResult<QuoteSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int page = PageRequest.FirstPage, [FromQuery] int pageSize = PageRequest.DefaultPageSize, [FromQuery] string? status = null, CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListQuotesQuery(page, pageSize, status), cancellationToken), Ok);

    [HttpGet("service-orders")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Orders without quote")]
    [EndpointDescription("Returns the open service orders that do not have a quote yet. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<OpenServiceOrderListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ServiceOrders(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListQuoteServiceOrdersQuery(), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Create quote")]
    [EndpointDescription("Creates the quote of a service order with the items the mechanic chose and the default payment settings. One quote per order: 409 quotes.alreadyExists. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(CreateQuoteBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new CreateQuoteCommand(request.ServiceOrderId), cancellationToken), result => StatusCode(StatusCodes.Status201Created, result));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Get quote")]
    [EndpointDescription("Returns the quote with customer (masked contact and consent), vehicle, diagnosis, items, discount, payment plan, approval, delivery and totals. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetQuoteQuery(id), cancellationToken), Ok);

    [HttpGet("{id:guid}/options")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Quote options")]
    [EndpointDescription("Returns active packages, services and parts (with stock and lead time), suggestions from the diagnosis, discount rules, the discount limit of the signed-in user, payment settings and expiration choices. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<QuoteOptionsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Options(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetQuoteOptionsQuery(id), cancellationToken), Ok);

    [HttpPut("{id:guid}/draft")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Save quote draft")]
    [EndpointDescription("Replaces items, discount mode (Automatic, Manual, None), manual discount and payment plan of an editable quote (pending and not sent, or sent and failed or expired). Locked: 409 quotes.locked. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Draft(Guid id, QuoteDraftInput request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SaveQuoteDraftCommand(id, request), cancellationToken), Ok);

    [HttpPost("{id:guid}/discount-approvals")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Request discount approval")]
    [EndpointDescription("Asks a manager to approve a manual discount above the limit of the signed-in user. Within the limit: 422 quotes.approvalNotNeeded. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<DiscountApprovalDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> RequestApproval(Guid id, DiscountApprovalBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RequestDiscountApprovalCommand(id, request.Type ?? string.Empty, request.Value, request.Reason ?? string.Empty), cancellationToken), result => StatusCode(StatusCodes.Status201Created, result));

    [HttpPost("{id:guid}/discount-approvals/{approvalId:guid}/approve")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Approve discount")]
    [EndpointDescription("Approves the pending discount request. Roles: Admin, Manager.")]
    [ProducesResponseType<DiscountApprovalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> ApproveDiscount(Guid id, Guid approvalId, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DecideDiscountApprovalCommand(id, approvalId, true), cancellationToken), Ok);

    [HttpPost("{id:guid}/discount-approvals/{approvalId:guid}/reject")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Reject discount")]
    [EndpointDescription("Rejects the pending discount request. Roles: Admin, Manager.")]
    [ProducesResponseType<DiscountApprovalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> RejectDiscount(Guid id, Guid approvalId, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DecideDiscountApprovalCommand(id, approvalId, false), cancellationToken), Ok);

    [HttpPost("{id:guid}/message-draft")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Draft quote message")]
    [EndpointDescription("Returns a ready message for the customer with first name, vehicle, plate, total and deadline. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<MessageDraftDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> MessageDraft(Guid id, MessageDraftBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DraftQuoteMessageQuery(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/send")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Send quote")]
    [EndpointDescription("Sends the quote by WhatsApp (returns the wa.me link), e-mail or link with a portal link that expires in the chosen days, and moves the service order to WaitingApproval. Needs customer consent for the channel (422 quotes.noConsent), items, positive total and, for a discount above the limit, an approved request (422 quotes.approvalRequired). Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<SendQuoteResultDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Send(Guid id, SendQuoteBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SendQuoteCommand(id, request.Channel ?? string.Empty, request.ExpiresInDays, request.Message ?? string.Empty), cancellationToken), result => Accepted(result));

    [HttpGet("{id:guid}/delivery")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Quote delivery")]
    [EndpointDescription("Returns the delivery status (Queued, Sent, Opened when the customer opens the portal, Failed). Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<QuoteDeliveryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Delivery(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetQuoteDeliveryQuery(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/delivery/retry")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Retry quote delivery")]
    [EndpointDescription("Sends again a failed or expired delivery with a new link valid for 7 days. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<QuoteDeliveryDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RetryQuoteDeliveryCommand(id), cancellationToken), result => Accepted(result));

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Approve quote at the desk")]
    [EndpointDescription("Approves the quote in person: the service order goes to InProgress and parts are deducted from stock. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ApproveQuoteAtDeskCommand(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Reject quote at the desk")]
    [EndpointDescription("Rejects the quote with a reason of 5 or more characters; the service order goes back to InDiagnosis. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Reject(Guid id, RejectQuoteBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RejectQuoteAtDeskCommand(id, request.Reason ?? string.Empty), cancellationToken), Ok);

    [HttpPost("{id:guid}/versions")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Create quote version")]
    [EndpointDescription("Creates a new pending version from the latest rejected version, copying items and discount. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> NewVersion(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new CreateQuoteVersionCommand(id), cancellationToken), result => StatusCode(StatusCodes.Status201Created, result));

    [HttpGet("{id:guid}/versions")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("List quote versions")]
    [EndpointDescription("Returns every version of the quote from the newest, with items and totals. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<QuoteVersionListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Versions(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListQuoteVersionsQuery(id), cancellationToken), Ok);
}
