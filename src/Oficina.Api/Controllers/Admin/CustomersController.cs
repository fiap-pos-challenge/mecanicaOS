using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Customers;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Commands.CreateCustomer;
using Oficina.Application.Customers.Commands.DeleteCustomer;
using Oficina.Application.Customers.Commands.UpdateCustomer;
using Oficina.Application.Customers.Dtos;
using Oficina.Application.Customers.Queries.GetCustomerById;
using Oficina.Application.Customers.Queries.ListCustomers;
using Oficina.Application.Customers.Queries.SearchCustomerByDocument;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/customers")]
[Tags("Clientes")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class CustomersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Listar clientes")]
    [EndpointDescription("Retorna uma página de clientes ordenados por nome. Perfis: Admin, Atendente.")]
    [ProducesResponseType<PagedResult<CustomerDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListCustomersQuery(page, pageSize), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Cadastrar cliente")]
    [EndpointDescription("Cadastra um cliente com CPF ou CNPJ, telefone e, opcionalmente, e-mail e endereço (endereço completo ou nenhum). Perfis: Admin, Atendente.")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand(request.Name, request.Document, request.Email, request.Phone, request.Address?.ToInput());
        return Respond(await sender.Send(command, cancellationToken), customer => CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Consultar cliente")]
    [EndpointDescription("Retorna um cliente pelo id. Perfis: Admin, Atendente.")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetCustomerByIdQuery(id), cancellationToken), Ok);

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Atualizar cliente")]
    [EndpointDescription("Atualiza nome, contato e endereço. O documento (CPF/CNPJ) não pode ser alterado. Perfis: Admin, Atendente.")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerCommand(id, request.Name, request.Email, request.Phone, request.Address?.ToInput());
        return Respond(await sender.Send(command, cancellationToken), Ok);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Excluir cliente")]
    [EndpointDescription("Exclui logicamente um cliente sem ordens de serviço abertas nem veículos cadastrados. Perfil: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeleteCustomerCommand(id), cancellationToken));

    [HttpPost("search")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Buscar cliente por documento")]
    [EndpointDescription("Busca um cliente pelo CPF ou CNPJ enviado no corpo da requisição (nunca na URL). Perfis: Admin, Atendente.")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Search(SearchCustomerRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SearchCustomerByDocumentQuery(request.Document), cancellationToken), Ok);
}
