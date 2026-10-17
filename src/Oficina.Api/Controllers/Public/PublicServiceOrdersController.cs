using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Contracts.ServiceOrders;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.ServiceOrders.Commands.ApproveQuoteByToken;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries.LookupServiceOrder;

namespace Oficina.Api.Controllers.Public;

[Route("api/public")]
[Tags("Public")]
[AllowAnonymous]
public sealed class PublicServiceOrdersController(ISender sender) : ApiControllerBase
{
    [HttpPost("service-orders/lookup")]
    [EndpointSummary("Track service order")]
    [EndpointDescription("Returns status, items and quote of a service order when the number and the customer CPF or CNPJ sent in the body match. Anonymous.")]
    [ProducesResponseType<PublicServiceOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Lookup(LookupServiceOrderRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new LookupServiceOrderQuery(request.Number, request.Document), cancellationToken), Ok);

    [HttpPost("quotes/{token}/approve")]
    [EndpointSummary("Approve quote by link")]
    [EndpointDescription("Approves the pending quote identified by the link token, deducts the parts from stock and moves the service order to InProgress in the same transaction. The token is single use. Anonymous.")]
    [ProducesResponseType<PublicServiceOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> ApproveQuote([FromRoute] string token, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ApproveQuoteByTokenCommand(token), cancellationToken), Ok);
}
