using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Catalog;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Catalog.Commands.CreateService;
using Oficina.Application.Catalog.Commands.DeactivateService;
using Oficina.Application.Catalog.Commands.ReactivateService;
using Oficina.Application.Catalog.Commands.UpdateService;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Queries.GetServiceById;
using Oficina.Application.Catalog.Queries.ListServices;
using Oficina.Application.Common.Pagination;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/services")]
[Tags("Serviços")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class ServicesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Listar serviços")]
    [EndpointDescription("Retorna uma página de serviços ordenados por nome. Lista apenas serviços ativos, a menos que includeInactive seja true. Perfis: Admin, Atendente, Mecânico.")]
    [ProducesResponseType<PagedResult<ServiceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListServicesQuery(page, pageSize, includeInactive), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Cadastrar serviço")]
    [EndpointDescription("Cadastra um serviço ativo no catálogo com nome, descrição opcional, preço base maior que zero e tempo estimado em minutos maior que zero. Perfil: Admin.")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(CreateServiceRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateServiceCommand(request.Name, request.Description, request.BasePrice, request.EstimatedMinutes);
        return Respond(await sender.Send(command, cancellationToken), service => CreatedAtAction(nameof(GetById), new { id = service.Id }, service));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Consultar serviço")]
    [EndpointDescription("Retorna um serviço pelo id, ativo ou inativo (ver isActive). Perfis: Admin, Atendente, Mecânico.")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetServiceByIdQuery(id), cancellationToken), Ok);

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Atualizar serviço")]
    [EndpointDescription("Atualiza nome, descrição, preço base e tempo estimado de um serviço ativo. Serviços inativos precisam ser reativados antes. Itens já adicionados a ordens de serviço mantêm o preço copiado. Perfil: Admin.")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateServiceCommand(id, request.Name, request.Description, request.BasePrice, request.EstimatedMinutes);
        return Respond(await sender.Send(command, cancellationToken), Ok);
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Desativar serviço")]
    [EndpointDescription("Oculta o serviço da listagem padrão, bloqueia alterações e impede que seja adicionado a novas ordens de serviço. Itens já adicionados são mantidos. Perfil: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeactivateServiceCommand(id), cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Reativar serviço")]
    [EndpointDescription("Volta a exibir um serviço inativo na listagem padrão e permite novamente alterações e uso em ordens de serviço. Perfil: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ReactivateServiceCommand(id), cancellationToken));
}
