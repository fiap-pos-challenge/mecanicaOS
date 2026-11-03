using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Inventory;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Inventory.Commands.CreatePart;
using Oficina.Application.Inventory.Commands.DeactivatePart;
using Oficina.Application.Inventory.Commands.ReactivatePart;
using Oficina.Application.Inventory.Commands.RegisterStockEntry;
using Oficina.Application.Inventory.Commands.UpdatePart;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Queries.GetPartById;
using Oficina.Application.Inventory.Queries.ListParts;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/parts")]
[Tags("Peças e Insumos")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class PartsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Listar peças e insumos")]
    [EndpointDescription("Retorna uma página de peças e insumos ordenados por nome, com saldo em estoque. Lista apenas itens ativos, a menos que includeInactive seja true. Com belowMinimum=true, lista só os itens abaixo do estoque mínimo. Perfis: Admin, Atendente, Mecânico.")]
    [ProducesResponseType<PagedResult<PartDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] bool includeInactive = false,
        [FromQuery] bool belowMinimum = false,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListPartsQuery(page, pageSize, includeInactive, belowMinimum), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Cadastrar peça ou insumo")]
    [EndpointDescription("Cadastra uma peça ou insumo ativo com código único, nome, tipo (Part ou Supply), unidade de medida, preço unitário e estoque mínimo. O estoque começa em zero; use a entrada de estoque para adicionar quantidade. Perfil: Admin.")]
    [ProducesResponseType<PartDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(CreatePartRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePartCommand(request.Code, request.Name, request.Type, request.UnitOfMeasure, request.UnitPrice, request.MinimumStock);
        return Respond(await sender.Send(command, cancellationToken), part => CreatedAtAction(nameof(GetById), new { id = part.Id }, part));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Consultar peça ou insumo")]
    [EndpointDescription("Retorna uma peça ou insumo pelo id (ativo ou inativo), com as movimentações de estoque (entradas e baixas por ordem de serviço) da mais antiga para a mais recente. Perfis: Admin, Atendente, Mecânico.")]
    [ProducesResponseType<PartDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetPartByIdQuery(id), cancellationToken), Ok);

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Atualizar peça ou insumo")]
    [EndpointDescription("Atualiza nome, tipo, unidade de medida, preço unitário e estoque mínimo. Código e saldo em estoque não são alterados aqui. Itens já adicionados a ordens de serviço mantêm o preço copiado. Perfil: Admin.")]
    [ProducesResponseType<PartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, UpdatePartRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePartCommand(id, request.Name, request.Type, request.UnitOfMeasure, request.UnitPrice, request.MinimumStock);
        return Respond(await sender.Send(command, cancellationToken), Ok);
    }

    [HttpPost("{id:guid}/stock-entries")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Registrar entrada de estoque")]
    [EndpointDescription("Adiciona quantidade ao estoque de uma peça ou insumo e registra a movimentação com o motivo e o usuário. Perfil: Admin.")]
    [ProducesResponseType<PartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> RegisterStockEntry(Guid id, RegisterStockEntryRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RegisterStockEntryCommand(id, request.Quantity, request.Reason), cancellationToken), Ok);

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Desativar peça ou insumo")]
    [EndpointDescription("Oculta o item da listagem padrão e impede que seja adicionado a novas ordens de serviço. Itens já adicionados são mantidos. Perfil: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeactivatePartCommand(id), cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Reativar peça ou insumo")]
    [EndpointDescription("Volta a exibir um item inativo na listagem padrão e permite adicioná-lo novamente a ordens de serviço. Perfil: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ReactivatePartCommand(id), cancellationToken));
}
