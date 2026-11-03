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
[Tags("Ordens de Serviço")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class ServiceOrdersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Listar ordens de serviço")]
    [EndpointDescription("Retorna uma página de ordens de serviço da mais recente para a mais antiga, com filtro opcional por status e por placa exata. Perfis: Admin, Atendente, Mecânico.")]
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
    [EndpointSummary("Abrir ordem de serviço")]
    [EndpointDescription("Abre uma ordem de serviço com status Received. Cliente identificado por CPF ou CNPJ e veículo pela placa, ambos no corpo da requisição. O veículo deve pertencer ao cliente e não pode ter outra ordem de serviço aberta. Perfis: Admin, Atendente.")]
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
    [EndpointSummary("Consultar ordem de serviço")]
    [EndpointDescription("Retorna uma ordem de serviço com cliente, veículo, serviços, peças, orçamento atual, histórico de status e datas. Perfis: Admin, Atendente, Mecânico.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetServiceOrderByIdQuery(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/diagnosis/start")]
    [Authorize(Policy = Policies.AdminOrMechanic)]
    [EndpointSummary("Iniciar diagnóstico")]
    [EndpointDescription("Move uma ordem de serviço de Received para InDiagnosis. Serviços e peças só podem ser adicionados ou removidos neste status. Perfis: Admin, Mecânico.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> StartDiagnosis([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new StartDiagnosisCommand(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/services")]
    [Authorize(Policy = Policies.AdminOrMechanic)]
    [EndpointSummary("Adicionar serviço")]
    [EndpointDescription("Adiciona um serviço ativo do catálogo a uma ordem de serviço em diagnóstico, copiando o nome e o preço base atuais. Perfis: Admin, Mecânico.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> AddService([FromRoute] Guid id, AddServiceItemRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new AddServiceItemCommand(id, request.ServiceId, request.Quantity), cancellationToken), Ok);

    [HttpPost("{id:guid}/parts")]
    [Authorize(Policy = Policies.AdminOrMechanic)]
    [EndpointSummary("Adicionar peça ou insumo")]
    [EndpointDescription("Adiciona uma peça ou insumo ativo, com estoque suficiente, a uma ordem de serviço em diagnóstico, copiando o nome e o preço unitário atuais. O estoque só é baixado quando o orçamento é aprovado. Perfis: Admin, Mecânico.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> AddPart([FromRoute] Guid id, AddPartItemRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new AddPartItemCommand(id, request.PartId, request.Quantity), cancellationToken), Ok);

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [Authorize(Policy = Policies.AdminOrMechanic)]
    [EndpointSummary("Remover item")]
    [EndpointDescription("Remove um item (serviço ou peça) de uma ordem de serviço em diagnóstico. Perfis: Admin, Mecânico.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> RemoveItem([FromRoute] Guid id, [FromRoute] Guid itemId, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RemoveItemCommand(id, itemId), cancellationToken), Ok);

    [HttpPost("{id:guid}/quote/send")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Enviar orçamento")]
    [EndpointDescription("Gera o orçamento a partir dos serviços e peças de uma ordem de serviço em diagnóstico e a move para WaitingApproval. Retorna o token de aprovação de uso único para enviar ao cliente, que aprova ou recusa pelas rotas públicas. Perfis: Admin, Atendente.")]
    [ProducesResponseType<SentQuoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> SendQuote([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SendQuoteCommand(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/quote/approve")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Aprovar orçamento")]
    [EndpointDescription("Aprova presencialmente o orçamento pendente, baixa as peças do estoque e move a ordem de serviço para InProgress na mesma transação. Perfis: Admin, Atendente.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> ApproveQuote([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ApproveQuoteCommand(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/quote/reject")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Recusar orçamento")]
    [EndpointDescription("Registra a recusa do orçamento pendente pelo cliente, com o motivo, e devolve a ordem de serviço para InDiagnosis para ajuste dos itens e envio de novo orçamento. Perfis: Admin, Atendente.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> RejectQuote([FromRoute] Guid id, RejectQuoteRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RejectQuoteCommand(id, request.Reason), cancellationToken), Ok);

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Policies.AdminOrMechanic)]
    [EndpointSummary("Finalizar ordem de serviço")]
    [EndpointDescription("Move uma ordem de serviço de InProgress para Completed, registra o horário de finalização (usado na métrica de tempo médio de execução) e notifica o cliente. Perfis: Admin, Mecânico.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Complete([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new CompleteServiceOrderCommand(id), cancellationToken), Ok);

    [HttpPost("{id:guid}/deliver")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Entregar veículo")]
    [EndpointDescription("Move uma ordem de serviço de Completed para Delivered. Após a entrega, o veículo pode receber uma nova ordem de serviço. Perfis: Admin, Atendente.")]
    [ProducesResponseType<ServiceOrderDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Deliver([FromRoute] Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeliverServiceOrderCommand(id), cancellationToken), Ok);
}
