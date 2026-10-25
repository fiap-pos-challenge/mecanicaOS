using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oficina.Api.Contracts.Portal;
using Oficina.Api.ExceptionHandling;
using Oficina.Api.Extensions;
using Oficina.Application.Documents.Dtos;
using Oficina.Application.Documents.Queries.GetDocument;
using Oficina.Application.Documents.Queries.GetDocumentPdf;
using Oficina.Application.Documents.Queries.VerifyDocument;
using Oficina.Application.Portal.Commands.ApproveQuoteFromPortal;
using Oficina.Application.Portal.Commands.ConfirmPixPayment;
using Oficina.Application.Portal.Commands.ConfirmPortalIdentity;
using Oficina.Application.Portal.Commands.CreatePixCharge;
using Oficina.Application.Portal.Commands.RefreshChargeStatus;
using Oficina.Application.Portal.Commands.RejectQuoteFromPortal;
using Oficina.Application.Portal.Commands.RequestCardPayment;
using Oficina.Application.Portal.Dtos;
using Oficina.Application.Portal.Queries.GetPortalAccess;
using Oficina.Application.Portal.Queries.GetPortalOrder;
using Oficina.Application.Portal.Queries.GetPortalPhoto;
using Oficina.Application.Portal.Queries.GetPortalSessionOrder;
using Oficina.Application.ServiceOrders.Dtos;

namespace Oficina.Api.Controllers.Public;

[Route("api/public")]
[EnableRateLimiting(SecurityExtensions.PublicRateLimitPolicy)]
[Tags("Portal")]
[AllowAnonymous]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, ProblemResponses.ContentType)]
public sealed class PublicPortalController(ISender sender) : ApiControllerBase
{
    public const string SessionHeader = "X-Portal-Session";
    public const string SignatureHeader = "X-Pix-Signature";

    [HttpPost("portal/access")]
    [EndpointSummary("Open portal link")]
    [EndpointDescription("Checks the link token sent in the body and returns how many confirmation attempts are left. 404 portal.linkUnavailable for unknown or expired links, 423 portal.locked after 3 wrong attempts. Anonymous.")]
    [ProducesResponseType<PortalAccessDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked, ProblemResponses.ContentType)]
    public async Task<IActionResult> Access(PortalTokenBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetPortalAccessQuery(request.Token), cancellationToken), Ok);

    [HttpPost("portal/confirm")]
    [EndpointSummary("Confirm portal identity")]
    [EndpointDescription("Confirms the customer with the last 3 digits of the phone and opens a 1-hour session sent back in the body. Wrong digits return WrongDigits with attempts left; the third wrong attempt locks the link (423). Anonymous.")]
    [ProducesResponseType<PortalConfirmDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked, ProblemResponses.ContentType)]
    public async Task<IActionResult> Confirm(PortalConfirmBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ConfirmPortalIdentityCommand(request.Token, request.LastDigits), cancellationToken), Ok);

    [HttpGet("portal/order")]
    [EndpointSummary("Portal order")]
    [EndpointDescription("Returns the service order of the portal session: status timeline, photos, quote items, payment and documents. Requires the X-Portal-Session header. Anonymous.")]
    [ProducesResponseType<PortalOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType)]
    public async Task<IActionResult> Order([FromHeader(Name = SessionHeader)] string? session, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetPortalOrderQuery(session), cancellationToken), Ok);

    [HttpGet("portal/photos/{photoId:guid}")]
    [EndpointSummary("Get portal photo")]
    [EndpointDescription("Returns one photo of the service order linked to the portal session (header X-Portal-Session). The order view only carries the photo path.")]
    [ProducesResponseType<ServiceOrderPhotoContentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Photo([FromHeader(Name = SessionHeader)] string? session, Guid photoId, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetPortalPhotoQuery(session, photoId), cancellationToken), Ok);

    [HttpPost("portal/quote/approve")]
    [EndpointSummary("Approve quote in portal")]
    [EndpointDescription("Approves the chosen quote items. Items left out stay out of the order; the order moves to InProgress. 409 portal.alreadyAnswered when the quote is not pending. Requires X-Portal-Session. Anonymous.")]
    [ProducesResponseType<PortalOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Approve([FromHeader(Name = SessionHeader)] string? session, PortalApproveBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ApproveQuoteFromPortalCommand(session, request.ItemIds ?? []), cancellationToken), Ok);

    [HttpPost("portal/quote/reject")]
    [EndpointSummary("Reject quote in portal")]
    [EndpointDescription("Rejects the quote with a reason of at least 5 characters and sends the order back to diagnosis. Requires X-Portal-Session. Anonymous.")]
    [ProducesResponseType<PortalOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Reject([FromHeader(Name = SessionHeader)] string? session, PortalRejectBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RejectQuoteFromPortalCommand(session, request.Reason), cancellationToken), Ok);

    [HttpPost("portal/payments/pix")]
    [EndpointSummary("Create Pix charge")]
    [EndpointDescription("Creates a 30-minute Pix charge for the approved amount with copy-and-paste code and QR code. 422 portal.nothingToPay or portal.pixUnavailable. Requires X-Portal-Session. Anonymous.")]
    [ProducesResponseType<PixChargeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Pix([FromHeader(Name = SessionHeader)] string? session, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new CreatePixChargeCommand(session), cancellationToken), Ok);

    [HttpGet("portal/payments/{id:guid}")]
    [EndpointSummary("Charge status")]
    [EndpointDescription("Returns Pending, Paid or Expired for a charge of the portal session. Requires X-Portal-Session. Anonymous.")]
    [ProducesResponseType<ChargeStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Charge([FromHeader(Name = SessionHeader)] string? session, Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RefreshChargeStatusCommand(session, id), cancellationToken), Ok);

    [HttpPost("portal/payments/card")]
    [EndpointSummary("Card checkout")]
    [EndpointDescription("Starts a card checkout when a card provider is configured; otherwise 422 portal.cardUnavailable. Requires X-Portal-Session. Anonymous.")]
    [ProducesResponseType<CardRedirectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Card([FromHeader(Name = SessionHeader)] string? session, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RequestCardPaymentCommand(session), cancellationToken), Ok);

    [HttpGet("portal/documents/{code}")]
    [EndpointSummary("Portal document")]
    [EndpointDescription("Returns a document of the portal session order. Requires X-Portal-Session. Anonymous.")]
    [ProducesResponseType<DocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Document([FromHeader(Name = SessionHeader)] string? session, string code, CancellationToken cancellationToken)
    {
        var order = await sender.Send(new GetPortalSessionOrderQuery(session), cancellationToken);
        return order.Error is { } error ? ProblemFor(error) : Respond(await sender.Send(new GetDocumentQuery(code, order.Value), cancellationToken), Ok);
    }

    [HttpGet("portal/documents/{code}/pdf")]
    [EndpointSummary("Portal document PDF")]
    [EndpointDescription("Returns the PDF of a document of the portal session order as a data URL. Requires X-Portal-Session. Anonymous.")]
    [ProducesResponseType<DocumentDownloadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> DocumentPdf([FromHeader(Name = SessionHeader)] string? session, string code, CancellationToken cancellationToken)
    {
        var order = await sender.Send(new GetPortalSessionOrderQuery(session), cancellationToken);
        return order.Error is { } error ? ProblemFor(error) : Respond(await sender.Send(new GetDocumentPdfQuery(code, order.Value), cancellationToken), Ok);
    }

    [HttpGet("documents/{code}/verification")]
    [EndpointSummary("Verify document")]
    [EndpointDescription("Tells whether a document code was issued by the shop, with kind, number, date and total. Anonymous.")]
    [ProducesResponseType<DocumentVerificationDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Verify(string code, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new VerifyDocumentQuery(code), cancellationToken), Ok);

    [HttpPost("payments/pix/webhook")]
    [EndpointSummary("Pix webhook")]
    [EndpointDescription("Marks a Pix charge as paid and issues the receipt. The X-Pix-Signature header carries the HMAC-SHA256 of the txId with the configured secret. Anonymous.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> PixWebhook([FromHeader(Name = SignatureHeader)] string? signature, PixWebhookBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ConfirmPixPaymentCommand(request.TxId, signature), cancellationToken));
}
