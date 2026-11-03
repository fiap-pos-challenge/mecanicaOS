using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Contracts.ServiceOrders;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.ServiceOrders.Commands.ApproveQuoteByToken;
using Oficina.Application.ServiceOrders.Commands.RejectQuoteByToken;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries.LookupServiceOrder;

namespace Oficina.Api.Controllers.Public;

[Route("api/public")]
[Tags("Público")]
[AllowAnonymous]
public sealed class PublicServiceOrdersController(ISender sender) : ApiControllerBase
{
    [HttpPost("service-orders/lookup")]
    [EndpointSummary("Acompanhar ordem de serviço")]
    [EndpointDescription("Retorna status, itens e orçamento de uma ordem de serviço quando o número e o CPF ou CNPJ do cliente enviados no corpo conferem. Acesso anônimo.")]
    [ProducesResponseType<PublicServiceOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Lookup(LookupServiceOrderRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new LookupServiceOrderQuery(request.Number, request.Document), cancellationToken), Ok);

    [HttpPost("quotes/{token}/approve")]
    [EndpointSummary("Aprovar orçamento pelo link")]
    [EndpointDescription("Aprova o orçamento pendente identificado pelo token do link, baixa as peças do estoque e move a ordem de serviço para InProgress na mesma transação. Token de uso único. Acesso anônimo.")]
    [ProducesResponseType<PublicServiceOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> ApproveQuote([FromRoute] string token, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ApproveQuoteByTokenCommand(token), cancellationToken), Ok);

    [HttpPost("quotes/{token}/reject")]
    [EndpointSummary("Recusar orçamento pelo link")]
    [EndpointDescription("Recusa o orçamento pendente identificado pelo token do link, com o motivo enviado no corpo, e devolve a ordem de serviço para InDiagnosis. Token de uso único. Acesso anônimo.")]
    [ProducesResponseType<PublicServiceOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> RejectQuote([FromRoute] string token, RejectQuoteRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RejectQuoteByTokenCommand(token, request.Reason), cancellationToken), Ok);
}
